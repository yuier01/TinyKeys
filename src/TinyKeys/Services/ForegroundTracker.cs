using TinyKeys.Interop;

namespace TinyKeys.Services;

/// <summary>
/// Keeps track of the last top-level window that belonged to another process.
/// A foreground-change hook is used so the panel can give the key back to the
/// application the user was using before clicking TinyKeys.
/// </summary>
internal sealed class ForegroundTracker : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly object _sync = new();
    private IntPtr _hook;
    private IntPtr _lastExternalWindow;

    public ForegroundTracker(IntPtr initialTarget)
    {
        _lastExternalWindow = initialTarget;
        _callback = OnForegroundChanged;

        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _callback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
    }

    public IntPtr LastExternalWindow
    {
        get
        {
            lock (_sync)
            {
                return _lastExternalWindow;
            }
        }
    }

    private void OnForegroundChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return;
        }

        lock (_sync)
        {
            _lastExternalWindow = hwnd;
        }
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
