using System;
using System.Windows.Forms;
using TinyKeys.Diagnostics;
using TinyKeys.Interop;

namespace TinyKeys;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        StartupTrace.Mark("Program.Main begin");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        StartupTrace.Mark("WinForms initialized");

        // Remember whichever application was active before TinyKeys appeared.
        var initialTarget = NativeMethods.GetForegroundWindow();
        StartupTrace.Mark("Initial target captured");

        var form = new MainForm(initialTarget);
        StartupTrace.Mark("MainForm constructed");

        Application.Run(form);
    }
}
