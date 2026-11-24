using System.Windows.Forms;
using TinyKeys.Interop;

namespace TinyKeys.Services;

/// <summary>
/// Places the panel on the half of a dual-monitor setup that is not currently
/// showing the browser. This keeps the panel visible when the browser is
/// full-screen without making the panel topmost.
/// </summary>
internal static class WindowPlacement
{
    private readonly struct MonitorInfo
    {
        public MonitorInfo(IntPtr handle, NativeMethods.RECT work, bool isPrimary)
        {
            Handle = handle;
            Work = work;
            IsPrimary = isPrimary;
        }

        public IntPtr Handle { get; }
        public NativeMethods.RECT Work { get; }
        public bool IsPrimary { get; }
    }

    public static void PositionAtStartup(IntPtr hwnd, IntPtr initialTarget)
    {
        var monitors = GetMonitors();
        if (monitors.Count == 0)
        {
            return;
        }

        var chosen = ChooseMonitor(monitors, initialTarget);
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

            for (var i = 0; i < monitors.Count; i++)
            {
                if (monitors[i].Handle != browserMonitor)
                {
                    return monitors[i];
                }
            }
        }

        if (NativeMethods.GetCursorPos(out var cursor))
        {
            var cursorMonitor = NativeMethods.MonitorFromPoint(
                cursor,
                NativeMethods.MONITOR_DEFAULTTONEAREST);

            for (var i = 0; i < monitors.Count; i++)
            {
                if (monitors[i].Handle == cursorMonitor)
                {
                    return monitors[i];
                }
            }
        }

        for (var i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].IsPrimary)
            {
                return monitors[i];
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
