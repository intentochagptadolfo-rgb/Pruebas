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
        try
        {
            if (!RequestAdminAccess())
            {
                System.Windows.MessageBox.Show("Admin access denied.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var vm = _sp.GetRequiredService<ConfigurationViewModel>();
            var window = new ConfigurationView { DataContext = vm };
            _ = vm.LoadCommand.ExecuteAsync(null);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error opening configuration: {ex.Message}", "Navigation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void ShowLogin()
    {
        var vm = _sp.GetRequiredService<LoginViewModel>();
        var window = new LoginView(vm);
        window.ShowDialog();
    }

    public bool RequestAdminAccess()
    {
        try
        {
            var login = _sp.GetRequiredService<LoginViewModel>();
            var window = new LoginView(login);
            return window.ShowDialog() == true &&
                   login.AuthenticatedUser?.Role == UserRole.Administrator;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error during admin authentication: {ex.Message}", "Authentication Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return false;
        }
    }
}
