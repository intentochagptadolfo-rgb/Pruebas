using System.IO;
using System.Windows;
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
/// DI composition root. Uses the Generic Host so logging, configuration and lifetime
/// follow the same conventions as ASP.NET services.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.SetBasePath(Directory.GetCurrentDirectory());
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseSerilog((ctx, services, lc) => lc
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext())
            .ConfigureServices((ctx, services) =>
            {
                services.AddInspectionInfrastructure(ctx.Configuration);
                services.AddInspectionApplication();

                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IUiDispatcher, WpfDispatcher>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<ConfigurationViewModel>();
                services.AddSingleton<LoginViewModel>();

                services.AddSingleton<MainView>();
            })
            .Build();

        await _host.StartAsync();

        var orchestrator = _host.Services.GetRequiredService<IInspectionOrchestrator>();
        var mes = _host.Services.GetRequiredService<IMesClient>();
        var keyence = _host.Services.GetRequiredService<IKeyenceClient>();

        await mes.StartAsync();
        await keyence.StartAsync();
        await orchestrator.StartAsync();

        var main = _host.Services.GetRequiredService<MainView>();
        main.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
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
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
