using System.Windows;
using System.Windows.Input;

namespace HyundaiTransys.VisionInspection.UI.Services;

public interface IKioskModeService
{
    /// <summary>True when the operator has NOT authenticated to exit.</summary>
    bool IsLocked { get; set; }

    void Apply(Window window);
}

/// <summary>
/// Turns a WPF <see cref="Window"/> into a line-side operator HMI:
///  - starts maximized + no resize
///  - always on top
///  - taskbar icon hidden (so the operator can't ALT-TAB out to Explorer)
///  - Alt+F4 / system menu close require admin password
///  - minimize requests are cancelled while locked
/// Exit is coordinated with <see cref="IsLocked"/>: a successful admin login
/// sets it to false before calling <see cref="Window.Close"/>.
/// </summary>
public sealed class KioskModeService : IKioskModeService
{
    public bool IsLocked { get; set; } = true;

    public void Apply(Window window)
    {
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.WindowState = WindowState.Maximized;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.Topmost = true;
        window.ShowInTaskbar = false;

        window.StateChanged += (_, _) =>
        {
            if (IsLocked && window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Maximized;
        };

        window.Closing += (_, e) =>
        {
            if (IsLocked) e.Cancel = true;
        };

        // Block Alt+F4 while locked.
        window.PreviewKeyDown += (_, e) =>
        {
            if (!IsLocked) return;
            var alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            if (alt && e.Key == Key.F4) e.Handled = true;
            if (alt && e.Key == Key.Tab) e.Handled = true;
            if (e.Key == Key.LWin || e.Key == Key.RWin) e.Handled = true;
        };
    }
}
