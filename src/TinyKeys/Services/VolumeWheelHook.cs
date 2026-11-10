using System.Runtime.InteropServices;
using TinyKeys.Interop;

namespace TinyKeys.Services;

/// <summary>
/// Listens for mouse-wheel messages while the application is inactive.
/// The callback decides whether the pointer is over the volume area; when it
/// returns true the wheel event is swallowed so the page underneath does not
/// also scroll.
/// </summary>
internal sealed class VolumeWheelHook : IDisposable
{
    private readonly Func<int, int, int, bool> _onWheel;
    private readonly NativeMethods.LowLevelMouseProc _callback;
    private IntPtr _hook;

    public VolumeWheelHook(Func<int, int, int, bool> onWheel)
    {
        _onWheel = onWheel;
        _callback = HookCallback;

        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _callback,
            NativeMethods.GetModuleHandle(null),
            0);
    }

    public bool IsInstalled => _hook != IntPtr.Zero;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam.ToInt64() == NativeMethods.WM_MOUSEWHEEL)
        {
            try
            {
                var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                var delta = unchecked((short)(info.mouseData >> 16));

                if (delta != 0 && _onWheel(info.pt.X, info.pt.Y, delta))
                {
                    // Handled: do not let the focused page react to this notch.
                    return (IntPtr)1;
                }
            }
            catch
            {
                // A malformed hook message must never break global mouse input.
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
