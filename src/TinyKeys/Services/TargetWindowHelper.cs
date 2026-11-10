using System.Diagnostics;
using TinyKeys.Interop;

namespace TinyKeys.Services;

internal static class TargetWindowHelper
{
    private static readonly HashSet<string> BrowserProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "brave",
        "vivaldi",
        "opera",
        "opera_gx",
        "chromium"
    };

    public static bool IsSupportedBrowser(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return BrowserProcessNames.Contains(process.ProcessName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Brings the previously active browser window to the foreground and waits
    /// briefly for Windows to finish the switch before sending a key.
    /// </summary>
    public static bool TryActivate(IntPtr hwnd, int timeoutMs = 100)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        if (NativeMethods.IsIconic(hwnd))
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        }

        if (NativeMethods.GetForegroundWindow() == hwnd)
        {
            return true;
        }

        NativeMethods.SetForegroundWindow(hwnd);

        var stopwatch = Stopwatch.StartNew();
        do
        {
            if (NativeMethods.GetForegroundWindow() == hwnd)
            {
                return true;
            }

            Thread.Sleep(5);
        }
        while (stopwatch.ElapsedMilliseconds < timeoutMs);

        return NativeMethods.GetForegroundWindow() == hwnd;
    }
}
