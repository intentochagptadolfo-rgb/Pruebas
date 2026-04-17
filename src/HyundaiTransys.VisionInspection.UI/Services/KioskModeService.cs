using System.Windows;
using System.Windows.Input;
using HyundaiTransys.VisionInspection.Core.Configuration;
using Microsoft.Extensions.Options;

namespace HyundaiTransys.VisionInspection.UI.Services;

public interface IKioskModeService
{
    /// <summary>
    /// Mirror of <see cref="AppSettings.IsDeveloperMode"/>, cached at construction.
    /// </summary>
    bool IsDeveloperMode { get; }

    /// <summary>
    /// True while the operator has NOT authenticated to exit. Kiosk mode starts
    /// locked; Developer mode starts unlocked (the X button just works).
    /// </summary>
    bool IsLocked { get; set; }

    void Apply(Window window);
}

/// <summary>
/// Two behaviours driven by <c>App:IsDeveloperMode</c> in appsettings.json:
///
///  • <b>Developer Mode (true)</b> — regular Windows chrome, maximized, the X
///    button closes, Alt+F4 / Alt+Tab / Win key all work. For laptop testing.
///
///  • <b>Production Kiosk Mode (false)</b> — full-screen borderless, Topmost,
///    taskbar hidden, minimize/close cancelled, Alt+F4 / Alt+Tab / Win keys
///    suppressed. The only exit is the "Exit (Admin)" button with credentials.
/// </summary>
public sealed class KioskModeService : IKioskModeService
{
    public bool IsDeveloperMode { get; }
    public bool IsLocked { get; set; }

    public KioskModeService(IOptionsMonitor<AppSettings> options)
    {
        IsDeveloperMode = options.CurrentValue.IsDeveloperMode;
        // Kiosk starts locked; the developer laptop starts unlocked.
        IsLocked = !IsDeveloperMode;
    }

    public void Apply(Window window)
    {
        if (IsDeveloperMode)
            ApplyDeveloperMode(window);
        else
            ApplyProductionKioskMode(window);
    }

    // ---------- Developer Mode (laptop) ----------

    private static void ApplyDeveloperMode(Window window)
    {
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.WindowState = WindowState.Maximized;
        window.WindowStyle = WindowStyle.SingleBorderWindow; // title bar + X
        window.ResizeMode = ResizeMode.CanResize;
        window.Topmost = false;
        window.ShowInTaskbar = true;
        window.Title += "  [DEVELOPER MODE]";
        // No Closing / PreviewKeyDown / StateChanged hooks are attached, so
        // Alt+F4, Alt+Tab, the Windows key and the X button behave normally.
    }

    // ---------- Production Kiosk Mode (Lenovo ThinkCentre) ----------

    private void ApplyProductionKioskMode(Window window)
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
