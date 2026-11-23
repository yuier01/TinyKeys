using System.Windows;
using TinyKeys.Diagnostics;
using TinyKeys.Interop;

namespace TinyKeys;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        StartupTrace.Mark("App.OnStartup begin");
        base.OnStartup(e);
        StartupTrace.Mark("App.OnStartup after base");

        // Remember whichever application was active before TinyKeys appeared.
        var initialTarget = NativeMethods.GetForegroundWindow();
        StartupTrace.Mark("Initial target captured");

        var window = new MainWindow(initialTarget);
        StartupTrace.Mark("MainWindow constructed");

        MainWindow = window;
        StartupTrace.Mark("Window.Show begin");
        window.Show();
        StartupTrace.Mark("Window.Show returned");
    }
}
