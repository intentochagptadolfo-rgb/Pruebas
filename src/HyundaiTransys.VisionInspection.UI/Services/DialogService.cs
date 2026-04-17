using System.Windows;

namespace HyundaiTransys.VisionInspection.UI.Services;

public interface IDialogService
{
    void Info(string message, string title = "Information");
    void Warn(string message, string title = "Warning");
    bool Confirm(string message, string title = "Confirm");
}

public sealed class DialogService : IDialogService
{
    public void Info(string message, string title = "Information") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Warn(string message, string title = "Warning") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public bool Confirm(string message, string title = "Confirm") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
