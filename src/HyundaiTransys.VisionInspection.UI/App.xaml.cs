using System.IO;
using System.Windows;
using System.Windows.Threading;
using HyundaiTransys.VisionInspection.Application;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Infrastructure;
using HyundaiTransys.VisionInspection.UI.Services;
using HyundaiTransys.VisionInspection.UI.ViewModels;
using HyundaiTransys.VisionInspection.UI.Views;
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

            // 3) Start adapters in dependency order.
            var mes = _host.Services.GetRequiredService<IMesClient>();
            var keyence = _host.Services.GetRequiredService<IKeyenceClient>();
            var orchestrator = _host.Services.GetRequiredService<IInspectionOrchestrator>();

            await mes.StartAsync();
            await keyence.StartAsync();
            await orchestrator.StartAsync();

            // 4) Show the operator HMI in kiosk mode.
            var kiosk = _host.Services.GetRequiredService<IKioskModeService>();
            var main = _host.Services.GetRequiredService<MainView>();
            main.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            kiosk.Apply(main);
            MainWindow = main;
            main.Show();
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
            Log.CloseAndFlush();
            _instanceGuard.Dispose();
            base.OnExit(e);
        }
    }

    private static IHost BuildHost() =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                var baseDir = AppContext.BaseDirectory;
                cfg.SetBasePath(baseDir);
                cfg.AddJsonFile(Path.Combine(baseDir, "appsettings.json"),
                                optional: false, reloadOnChange: true);
            })
            .UseSerilog((ctx, _, lc) => lc
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext())
            .ConfigureServices((ctx, services) =>
            {
                services.AddInspectionInfrastructure(ctx.Configuration);
                services.AddInspectionApplication();

                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IUiDispatcher, WpfDispatcher>();
                services.AddSingleton<IKioskModeService, KioskModeService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<ConfigurationViewModel>();
                services.AddSingleton<LoginViewModel>();

                services.AddSingleton<MainView>();
            })
            .Build();

    // ---------- global exception hooks ----------

    private void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashLogger.Log("Dispatcher", e.Exception);
        TryLogStructured("Dispatcher", e.Exception);
        // Swallow to keep the HMI alive; operator keeps running, engineers see the log.
        e.Handled = true;
    }

    private void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            CrashLogger.Log($"AppDomain (terminating={e.IsTerminating})", ex);
            TryLogStructured("AppDomain", ex);
        }
    }

    private void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashLogger.Log("TaskScheduler", e.Exception);
        TryLogStructured("TaskScheduler", e.Exception);
        e.SetObserved();
    }

    private void TryLogStructured(string source, Exception ex)
    {
        try { _host?.Services.GetService<Serilog.ILogger>()?.Error(ex, "Unhandled from {Source}", source); }
        catch { /* crash logger is always-on fallback */ }
    }
}
