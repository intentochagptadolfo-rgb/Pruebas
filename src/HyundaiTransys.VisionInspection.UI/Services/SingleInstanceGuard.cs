using System.Runtime.InteropServices;

namespace HyundaiTransys.VisionInspection.UI.Services;

/// <summary>
/// Named Mutex guard that prevents two instances of the app from running in the
/// same Windows session (which would collide on the MES/Keyence TCP ports).
/// If another instance is detected, its main window is brought to the foreground.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    // Local\ prefix scopes the mutex to the Windows session (1 HMI per session).
    // Use Global\ only if the app must be unique across terminal-server sessions.
    private const string MutexName = @"Local\HyundaiTransys.VisionInspection.SingleInstance";
    private const string MainWindowTitle = "Hyundai Transys — Vision Inspection";

    private Mutex? _mutex;
    public bool IsOwner { get; private set; }

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        IsOwner = createdNew;
        return createdNew;
    }

    public static void BringExistingInstanceToFront()
    {
        var hwnd = FindWindow(null, MainWindowTitle);
        if (hwnd == IntPtr.Zero) return;

        if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
        SetForegroundWindow(hwnd);
    }

    public void Dispose()
    {
        if (_mutex is null) return;
        try { if (IsOwner) _mutex.ReleaseMutex(); } catch { /* swallow */ }
        _mutex.Dispose();
        _mutex = null;
    }

    // ---------- Win32 ----------
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
