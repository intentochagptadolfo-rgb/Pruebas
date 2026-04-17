using System.Windows;
using System.Windows.Controls;
using HyundaiTransys.VisionInspection.UI.ViewModels;

namespace HyundaiTransys.VisionInspection.UI.Views;

public partial class LoginView : Window
{
    public LoginView() => InitializeComponent();

    private void Pwd_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && vm.AuthenticatedUser is not null)
        {
            DialogResult = true;
            Close();
        }
    }
}
