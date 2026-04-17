using System.IO;
using System.Text;

namespace HyundaiTransys.VisionInspection.UI.Services;

/// <summary>
/// Ultra-resilient, dependency-free crash logger. Works BEFORE the DI host/Serilog
/// is built and keeps working if they tear down. Writes one file per day under
/// %ProgramData%\HTVision\crash (writable without elevation).
/// </summary>
public static class CrashLogger
{
    private static readonly object _gate = new();

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "HTVision", "crash");

    public static void Log(string source, Exception ex) =>
        Log(source, ex.ToString());

    public static void Log(string source, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"crash-{DateTime.Now:yyyyMMdd}.log");
            var entry = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                .Append(" | ").Append(source)
                .Append(" | ").AppendLine(message)
                .AppendLine(new string('-', 80))
                .ToString();

            lock (_gate)
            {
                File.AppendAllText(path, entry, Encoding.UTF8);
            }
        }
        catch
        {
            // Last-resort sink must never throw.
        }
    }
}
