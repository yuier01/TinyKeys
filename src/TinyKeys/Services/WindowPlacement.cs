using System.Windows;
using System.Windows.Interop;
using TinyKeys.Interop;

namespace TinyKeys.Services;

/// <summary>
/// Places the panel on the half of a dual-monitor setup that is not currently
/// showing the browser. This keeps the panel visible when the browser is
/// full-screen without making the panel topmost.
/// </summary>
internal static class WindowPlacement
{
    private readonly record struct MonitorInfo(IntPtr Handle, NativeMethods.RECT Work, bool IsPrimary);

    public static void PositionAtStartup(Window window, IntPtr initialTarget)
    {
        var monitors = GetMonitors();
        if (monitors.Count == 0)
        {
            return;
        }

        var chosen = ChooseMonitor(monitors, initialTarget);
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !NativeMethods.GetWindowRect(hwnd, out var windowRect))
        {
            return;
        }

        var width = windowRect.Right - windowRect.Left;
        var height = windowRect.Bottom - windowRect.Top;
        var work = chosen.Work;
        var x = work.Left + Math.Max(0, (work.Right - work.Left - width) / 2);
        var y = work.Top + Math.Max(0, (work.Bottom - work.Top - height) / 2);

        NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            x,
            y,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
    }

    private static MonitorInfo ChooseMonitor(
        IReadOnlyList<MonitorInfo> monitors,
        IntPtr initialTarget)
    {
        if (TargetWindowHelper.IsSupportedBrowser(initialTarget))
        {
            var browserMonitor = NativeMethods.MonitorFromWindow(
                initialTarget,
                NativeMethods.MONITOR_DEFAULTTONEAREST);

            var other = monitors.FirstOrDefault(m => m.Handle != browserMonitor);
            if (other.Handle != IntPtr.Zero)
            {
                return other;
            }
        }

        if (NativeMethods.GetCursorPos(out var cursor))
        {
            var cursorMonitor = NativeMethods.MonitorFromPoint(
                cursor,
                NativeMethods.MONITOR_DEFAULTTONEAREST);

            var atCursor = monitors.FirstOrDefault(m => m.Handle == cursorMonitor);
            if (atCursor.Handle != IntPtr.Zero)
            {
                return atCursor;
            }
        }

        foreach (var monitor in monitors)
        {
            if (monitor.IsPrimary)
            {
                return monitor;
            }
        }

        return monitors[0];
    }

    private static List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        NativeMethods.EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (hMonitor, _, _, _) =>
            {
                var info = new NativeMethods.MONITORINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>()
                };

                if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
                {
                    monitors.Add(new MonitorInfo(
                        hMonitor,
                        info.rcWork,
                        (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0));
                }

                return true;
            },
            IntPtr.Zero);

        return monitors;
    }
}
