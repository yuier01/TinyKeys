using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TinyKeys.Controls;
using TinyKeys.Interop;
using TinyKeys.Services;

namespace TinyKeys;

public partial class MainWindow : Window
{
    private const int HoldPollIntervalMilliseconds = 15;
    private const int MaxKeyRepeats = 6000;

    private readonly IntPtr _initialTarget;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly DispatcherTimer _holdTimer;
    private readonly DispatcherTimer _hintTimer;
    private readonly Brush _defaultHintBrush;

    private VolumeWheelHook? _volumeWheelHook;
    private HoldButton? _heldButton;
    private ushort _heldVirtualKey;
    private IntPtr _heldTarget;
    private bool _keyIsDown;
    private long _nextRepeatTick;
    private int _repeatIntervalMilliseconds;
    private int _repeatCount;
    private int _wheelAccumulator;
    private IntPtr _windowHandle;

    public MainWindow(IntPtr initialTarget)
    {
        InitializeComponent();

        _initialTarget = initialTarget;
        _defaultHintBrush = HintText.Foreground;
        _foregroundTracker = new ForegroundTracker(initialTarget);

        _holdTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(HoldPollIntervalMilliseconds)
        };
        _holdTimer.Tick += HoldTimer_Tick;

        _hintTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2.4)
        };
        _hintTimer.Tick += HintTimer_Tick;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowPlacement.PositionAtStartup(this, _initialTarget);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        _volumeWheelHook = new VolumeWheelHook(HandleVolumeWheel);

        if (!_volumeWheelHook.IsInstalled)
        {
            ShowHint("滚轮音量初始化失败，请重启 TinyKeys", isError: true);
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        ReleaseHeldKey();
        _hintTimer.Stop();
        _foregroundTracker.Dispose();
        _volumeWheelHook?.Dispose();
        _volumeWheelHook = null;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            ReleaseHeldKey();
        }
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            ReleaseHeldKey();
        }
    }

    private void KeyButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not HoldButton button)
        {
            return;
        }

        StartHold(button);
    }

    private void StartHold(HoldButton button)
    {
        ReleaseHeldKey();

        if (button.VirtualKey is <= 0 or > 0xFF)
        {
            return;
        }

        var target = _foregroundTracker.LastExternalWindow;
        if (!TargetWindowHelper.IsSupportedBrowser(target))
        {
            ShowHint("请先点一下 B 站视频，再使用控制条", isError: true);
            return;
        }

        if (!TargetWindowHelper.TryActivate(target) ||
            NativeMethods.GetForegroundWindow() != target)
        {
            ShowHint("无法切回浏览器，请再点一下 B 站视频", isError: true);
            return;
        }

        var virtualKey = (ushort)button.VirtualKey;

        // This is a real hold, not a series of clicks: send one key-down now,
        // repeat key-downs at the system keyboard rate while the mouse is held,
        // and send the matching key-up only when the mouse is released.
        // All buttons — including both arrow keys — use this exact same path;
        // only VirtualKey differs. Bilibili decides whether the held key means
        // fast-rewind (left) or 2x fast-forward (right).
        if (!KeyboardSender.SendKeyDown(virtualKey))
        {
            ShowHint("按键发送失败", isError: true);
            return;
        }

        var timings = KeyboardRepeatTimings.FromSystem();

        _keyIsDown = true;
        _heldButton = button;
        _heldVirtualKey = virtualKey;
        _heldTarget = target;
        _repeatCount = 0;
        _repeatIntervalMilliseconds = timings.IntervalMilliseconds;
        _nextRepeatTick = Environment.TickCount64 + timings.DelayMilliseconds;

        button.IsHeld = true;
        _holdTimer.Start();
    }

    private void HoldTimer_Tick(object? sender, EventArgs e)
    {
        if (!_keyIsDown || _heldButton is null)
        {
            ReleaseHeldKey();
            return;
        }

        if (WindowState == WindowState.Minimized ||
            !NativeMethods.IsWindow(_heldTarget) ||
            NativeMethods.GetForegroundWindow() != _heldTarget ||
            (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) == 0)
        {
            ReleaseHeldKey();
            return;
        }

        var now = Environment.TickCount64;
        if (now < _nextRepeatTick)
        {
            return;
        }

        // Keep the key logically down: only repeat key-down events here.
        if (!KeyboardSender.SendKeyDown(_heldVirtualKey))
        {
            ReleaseHeldKey();
            ShowHint("按键发送失败", isError: true);
            return;
        }

        _repeatCount++;
        _nextRepeatTick = now + _repeatIntervalMilliseconds;

        if (_repeatCount >= MaxKeyRepeats)
        {
            ReleaseHeldKey();
        }
    }

    private void ReleaseHeldKey()
    {
        _holdTimer.Stop();

        if (_keyIsDown)
        {
            _keyIsDown = false;
            KeyboardSender.SendKeyUp(_heldVirtualKey);
        }

        if (_heldButton is not null)
        {
            _heldButton.IsHeld = false;
        }

        _heldButton = null;
        _heldVirtualKey = 0;
        _heldTarget = IntPtr.Zero;
        _repeatCount = 0;
    }

    private void ShowHint(string message, bool isError)
    {
        HintText.Text = message;
        HintText.Foreground = isError
            ? (Brush)FindResource("ErrorTextBrush")
            : _defaultHintBrush;

        _hintTimer.Stop();
        _hintTimer.Start();
    }

    private void HintTimer_Tick(object? sender, EventArgs e)
    {
        _hintTimer.Stop();
        HintText.Text = "长按保持按键 · 滚轮调节系统音量";
        HintText.Foreground = _defaultHintBrush;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button can be released before the drag operation starts.
        }
    }

    private void PinButton_StateChanged(object sender, RoutedEventArgs e)
    {
        if (PinButton is null)
        {
            return;
        }

        var pinned = PinButton.IsChecked == true;
        Topmost = pinned;
        PinButton.ToolTip = pinned ? "取消置顶" : "置顶窗口";

        if (!pinned)
        {
            return;
        }

        // Raise the window to the top of the topmost group without stealing
        // focus from the browser that is currently being controlled.
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.SetWindowPos(
                hwnd,
                new IntPtr(-1), // HWND_TOPMOST
                0,
                0,
                0,
                0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void VolumeZone_MouseLeave(object sender, MouseEventArgs e)
    {
        _wheelAccumulator = 0;
    }

    private void VolumeZone_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // The low-level hook normally swallows this event before WPF sees it.
        // Keep a fallback in case the global hook could not be installed.
        if (_volumeWheelHook?.IsInstalled == true)
        {
            return;
        }

        ApplyVolumeDelta(e.Delta);
        e.Handled = true;
    }

    private bool HandleVolumeWheel(int screenX, int screenY, int delta)
    {
        if (!IsLoaded ||
            !IsVisible ||
            WindowState == WindowState.Minimized ||
            !IsPointOverVolumeZone(screenX, screenY))
        {
            return false;
        }

        ApplyVolumeDelta(delta);
        return true;
    }

    private bool IsPointOverVolumeZone(int screenX, int screenY)
    {
        if (VolumeZone.ActualWidth <= 0 || VolumeZone.ActualHeight <= 0)
        {
            return false;
        }

        try
        {
            var topLeft = VolumeZone.PointToScreen(new Point(0, 0));
            var bottomRight = VolumeZone.PointToScreen(
                new Point(VolumeZone.ActualWidth, VolumeZone.ActualHeight));

            if (screenX < topLeft.X || screenX > bottomRight.X ||
                screenY < topLeft.Y || screenY > bottomRight.Y)
            {
                return false;
            }

            var hit = NativeMethods.WindowFromPoint(new NativeMethods.POINT(screenX, screenY));
            if (hit == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.GetAncestor(hit, NativeMethods.GA_ROOT) == _windowHandle;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ApplyVolumeDelta(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        _wheelAccumulator += delta;

        var steps = Math.Min(Math.Abs(_wheelAccumulator) / NativeMethods.WHEEL_DELTA, 4);
        if (steps == 0)
        {
            return;
        }

        var increase = _wheelAccumulator > 0;
        for (var i = 0; i < steps; i++)
        {
            KeyboardSender.SendVolumeStep(increase);
        }

        _wheelAccumulator -= Math.Sign(_wheelAccumulator) * steps * NativeMethods.WHEEL_DELTA;
    }
}
