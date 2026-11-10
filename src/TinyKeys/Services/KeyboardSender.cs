using System.Runtime.InteropServices;
using TinyKeys.Interop;

namespace TinyKeys.Services;

internal static class KeyboardSender
{
    private static readonly int InputSize = Marshal.SizeOf<NativeMethods.INPUT>();

    public static bool SendKeyDown(ushort virtualKey)
    {
        return SendSingle(virtualKey, keyUp: false);
    }

    public static bool SendKeyUp(ushort virtualKey)
    {
        return SendSingle(virtualKey, keyUp: true);
    }

    public static bool SendKey(ushort virtualKey)
    {
        return SendKeyDown(virtualKey) && SendKeyUp(virtualKey);
    }

    public static void SendVolumeStep(bool increase)
    {
        SendKey(increase ? NativeMethods.VK_VOLUME_UP : NativeMethods.VK_VOLUME_DOWN);
    }

    private static bool SendSingle(ushort virtualKey, bool keyUp)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        var inputs = new[] { input };
        return NativeMethods.SendInput(1, inputs, InputSize) == 1;
    }
}
