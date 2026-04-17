using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.UI.ViewModels;
using HyundaiTransys.VisionInspection.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HyundaiTransys.VisionInspection.UI.Services;

public interface INavigationService
{
    void ShowConfiguration();
    void ShowLogin();

    /// <summary>Shows the login dialog and returns true iff the user authenticated as Administrator.</summary>
    bool RequestAdminAccess();
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _sp;

    public NavigationService(IServiceProvider sp) => _sp = sp;

    public void ShowConfiguration()
    {
        if (!RequestAdminAccess()) return;

        var vm = _sp.GetRequiredService<ConfigurationViewModel>();
        var window = new ConfigurationView { DataContext = vm };
        _ = vm.LoadCommand.ExecuteAsync(null);
        window.ShowDialog();
    }

    public void ShowLogin()
    {
        var vm = _sp.GetRequiredService<LoginViewModel>();
        var window = new LoginView { DataContext = vm };
        window.ShowDialog();
    }

    public bool RequestAdminAccess()
    {
        var login = _sp.GetRequiredService<LoginViewModel>();
        var window = new LoginView { DataContext = login };
        return window.ShowDialog() == true &&
               login.AuthenticatedUser?.Role == UserRole.Administrator;
    }
}
