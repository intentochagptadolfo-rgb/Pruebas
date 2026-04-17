using System.Windows;

namespace HyundaiTransys.VisionInspection.UI.Services;

public interface IUiDispatcher
{
    void Invoke(Action action);
}

public sealed class WpfDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app is null) { action(); return; }
        if (app.Dispatcher.CheckAccess()) action();
        else app.Dispatcher.Invoke(action);
    }
}
