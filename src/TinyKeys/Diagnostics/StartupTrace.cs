using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TinyKeys.Diagnostics;

/// <summary>
/// Lightweight startup timing log for diagnosing cold starts.
/// The log contains timing and environment information only; no input,
/// window titles, browsing data, or other content.
/// </summary>
internal static class StartupTrace
{
    private const long MaxLogBytes = 512 * 1024;

    private static readonly object Sync = new();
    private static readonly Stopwatch TraceClock = Stopwatch.StartNew();
    private static readonly DateTime ProcessStart;
    private static readonly string? LogPath;
    private static bool _disabled;
    private static long _lastTraceMilliseconds;

    static StartupTrace()
    {
        try
        {
            ProcessStart = Process.GetCurrentProcess().StartTime;
        }
        catch
        {
            ProcessStart = DateTime.Now;
        }

        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TinyKeys",
                "logs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "startup.log");
            RotateIfNeeded(path);
            LogPath = path;

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var header = new StringBuilder()
                .AppendLine($"===== TinyKeys {version} startup =====")
                .AppendLine($"When: {DateTimeOffset.Now:O}")
                .AppendLine($"Process start: {ProcessStart:O}")
                .AppendLine($"OS: {RuntimeInformation.OSDescription}")
                .AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}")
                .ToString();

            File.AppendAllText(path, header, Encoding.UTF8);
        }
        catch
        {
            _disabled = true;
            LogPath = null;
        }
    }

    public static void Mark(string stage)
    {
        if (_disabled || LogPath is null)
        {
            return;
        }

        try
        {
            var elapsed = TraceClock.ElapsedMilliseconds;
            var sinceProcess = Math.Max(0, (long)(DateTime.Now - ProcessStart).TotalMilliseconds);

            lock (Sync)
            {
                var delta = elapsed - _lastTraceMilliseconds;
                _lastTraceMilliseconds = elapsed;
                var line = $"{DateTimeOffset.Now:HH:mm:ss.fff} | trace={elapsed,6} ms | delta={delta,5} ms | sinceProcess={sinceProcess,6} ms | {stage}{Environment.NewLine}";
                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            _disabled = true;
        }
    }

    private static void RotateIfNeeded(string path)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length <= MaxLogBytes)
            {
                return;
            }

            File.Copy(path, path + ".old", overwrite: true);
            File.WriteAllText(path, string.Empty, Encoding.UTF8);
        }
        catch
        {
            // Rotation is best effort; do not prevent startup logging.
        }
    }
}
