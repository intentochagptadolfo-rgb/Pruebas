using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using HyundaiTransys.VisionInspection.Application;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.Infrastructure;
using HyundaiTransys.VisionInspection.Infrastructure.Persistence;
using HyundaiTransys.VisionInspection.UI.Services;
using HyundaiTransys.VisionInspection.UI.ViewModels;
using HyundaiTransys.VisionInspection.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;



namespace HyundaiTransys.VisionInspection.UI;

/// <summary>
/// Composition root + industrial desktop guarantees:
///  - single-instance lock (named Mutex) before any socket is opened
///  - pre-DI crash logger always-on (%ProgramData%\HTVision\crash)
///  - three global exception hooks (UI dispatcher, AppDomain, TaskScheduler)
///  - ordered start/stop of MES → Keyence → Orchestrator
///  - kiosk-mode main window.
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly SingleInstanceGuard _instanceGuard = new();
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // 1) Global exception hooks first — they must survive a partial boot.
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;
        DispatcherUnhandledException += OnDispatcherUnhandled;

        // 2) Enforce single instance BEFORE touching TCP ports or the DB.
        if (!_instanceGuard.TryAcquire())
        {
            CrashLogger.Log("Startup", "Another instance is already running; exiting.");
            SingleInstanceGuard.BringExistingInstanceToFront();
            Shutdown(exitCode: 0);
            return;
        }

        base.OnStartup(e);

        try
        {
            _host = BuildHost();
            await _host.StartAsync();
            await InitializeDatabaseAsync();

            // 3) Show the operator HMI in kiosk mode FIRST (non-blocking UI).
            var kiosk = _host.Services.GetRequiredService<IKioskModeService>();
            var main = _host.Services.GetRequiredService<MainView>();
            main.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            kiosk.Apply(main);
            MainWindow = main;
            main.Show();

            // 4) Start adapters asynchronously in background (fire-and-forget).
            // Connections attempt in parallel; failures are logged but don't crash.
            _ = Task.Run(async () =>
            {
                try
                {
                    var mes = _host.Services.GetRequiredService<IMesClient>();
                    var keyence = _host.Services.GetRequiredService<IKeyenceClient>();
                    var orchestrator = _host.Services.GetRequiredService<IInspectionOrchestrator>();

                    await mes.StartAsync();
                    await keyence.StartAsync();
                    await orchestrator.StartAsync();
                }
                catch (Exception ex)
                {
                    // Silent failure — watchdog handles reconnections.
                    CrashLogger.Log("BackgroundStart", ex);
                }
            });
        }
        catch (Exception ex)
        {
            CrashLogger.Log("OnStartup", ex);
            MessageBox.Show(
                "The application could not start. Engineering has been notified.\n\n" +
                $"Error: {ex.Message}",
                "Hyundai Transys — Vision Inspection",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(exitCode: 1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                var orchestrator = _host.Services.GetRequiredService<IInspectionOrchestrator>();
                var mes = _host.Services.GetRequiredService<IMesClient>();
                var keyence = _host.Services.GetRequiredService<IKeyenceClient>();

                await orchestrator.StopAsync();
                await mes.StopAsync();
                await keyence.StopAsync();

                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("OnExit", ex);
        }
        finally
        {
            _instanceGuard.Dispose();
        }
    }

    private IHost BuildHost()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddInspectionApplication();
                services.AddInspectionInfrastructure(config);
                // UI services registration
                services.AddSingleton<IKioskModeService, KioskModeService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IUiDispatcher, WpfDispatcher>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddTransient<MainView>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<ConfigurationView>();
                services.AddTransient<ConfigurationViewModel>();
                services.AddTransient<LoginView>();
                services.AddTransient<LoginViewModel>();
                services.Configure<AppSettings>(config.GetSection(AppSettings.SectionName));
            })
            .UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration))
            .Build();
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (_host is null)
            throw new InvalidOperationException("Host is not initialized.");

        using var scope = _host.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var dbContext = provider.GetRequiredService<InspectionDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var userService = provider.GetRequiredService<IUserService>();
        var users = await userService.ListAsync(cancellationToken);
        if (!users.Any())
        {
            await userService.CreateAsync("admin", "admin", UserRole.Administrator, cancellationToken);
        }
    }

    private void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e) =>
        CrashLogger.Log("AppDomain", e.ExceptionObject as Exception ?? new Exception("Unknown"));

    private void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashLogger.Log("TaskScheduler", e.Exception);
        e.SetObserved();
    }

    private void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashLogger.Log("Dispatcher", e.Exception);
        e.Handled = true;
    }
}