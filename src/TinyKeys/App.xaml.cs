using System.Windows;
using TinyKeys.Interop;

namespace TinyKeys;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Remember whichever application was active before TinyKeys appeared.
        var initialTarget = NativeMethods.GetForegroundWindow();
        var window = new MainWindow(initialTarget);
        MainWindow = window;
        window.Show();
    }
}
