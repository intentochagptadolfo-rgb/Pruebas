using System.Windows;
using System.Windows.Controls;
using HyundaiTransys.VisionInspection.UI.ViewModels;

namespace HyundaiTransys.VisionInspection.UI.Views;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Pwd_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Mensaje de prueba (Descomenta la siguiente línea si quieres confirmar que el clic sirve)
            System.Windows.MessageBox.Show("¡El botón sí funciona! Consultando BD...", "Test");

            if (DataContext is LoginViewModel vm)
            {
                vm.UserName = txtUsername.Text;
                vm.Password = txtPassword.Password;

                bool success = await vm.LoginAsync(vm.UserName, vm.Password);
                if (success)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    System.Windows.MessageBox.Show("Usuario o contraseña incorrecto.", "Login Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Error interno: DataContext es nulo.", "Login Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            // ¡Esta es la trampa! Si la base de datos falla, lo atraparemos aquí.
            System.Windows.MessageBox.Show($"El programa chocó al intentar leer la BD. Detalle: {ex.Message}", "Error Fatal Invisible", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        
    }
}