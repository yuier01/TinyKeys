using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TinyKeys.Diagnostics;
using TinyKeys.Interop;
using TinyKeys.Services;
using TinyKeys.Ui;

namespace TinyKeys;

internal sealed class MainForm : Form
{
    private const int DesignWidth = 440;
    private const int DesignHeight = 152;
    private const int HoldPollIntervalMilliseconds = 15;
    private const int MaxKeyRepeats = 6000;

    private readonly IntPtr _initialTarget;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _hintTimer;
    private readonly ToolTip _toolTip;

    private readonly Font _titleFont;
    private readonly Font _arrowFont;
    private readonly Font _spaceFont;
    private readonly Font _captionFont;
    private readonly Font _volumeFont;
    private readonly Font _hintFont;

    private readonly KeyButton _backButton;
    private readonly KeyButton _spaceButton;
    private readonly KeyButton _forwardButton;
    private readonly VolumeZone _volumeZone;
    private readonly IconButton _pinButton;
    private readonly IconButton _minimizeButton;
    private readonly IconButton _closeButton;
    private readonly Label _hintLabel;

    private VolumeWheelHook? _volumeWheelHook;
    private KeyButton? _heldButton;
    private ushort _heldVirtualKey;
    private IntPtr _heldTarget;
    private bool _keyIsDown;
    private Stopwatch? _holdClock;
    private int _nextRepeatMilliseconds;
    private int _repeatIntervalMilliseconds;
    private int _repeatCount;
    private int _wheelAccumulator;
    private bool _pinned;
    private bool _firstPaintLogged;
    private bool _firstShownLogged;
    private Region? _roundedRegion;

    private readonly string _defaultHint = "长按保持按键 · 滚轮调节系统音量";
    private readonly Color _defaultHintColor = Theme.TextMuted;

    public MainForm(IntPtr initialTarget)
    {
        StartupTrace.Mark("MainForm ctor begin");
        _initialTarget = initialTarget;

        SuspendLayout();

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "TinyKeys";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        ShowIcon = false;
        TopMost = false;
        MaximizeBox = false;
        MinimizeBox = true;
        BackColor = Color.White;
        ClientSize = new Size(DesignWidth, DesignHeight);
        Font = new Font(Theme.UiFontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        _titleFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        _arrowFont = new Font(Theme.SymbolFontFamily, 18F, FontStyle.Bold, GraphicsUnit.Point);
        _spaceFont = new Font(Theme.UiFontFamily, 12F, FontStyle.Bold, GraphicsUnit.Point);
        _captionFont = new Font(Theme.UiFontFamily, 7.5F, FontStyle.Regular, GraphicsUnit.Point);
        _volumeFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
        _hintFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);

        _pinButton = CreateHeaderButton(HeaderIconKind.Pin, 342, "切换窗口置顶");
        _minimizeButton = CreateHeaderButton(HeaderIconKind.Minimize, 370, "最小化 TinyKeys");
        _closeButton = CreateHeaderButton(HeaderIconKind.Close, 398, "退出 TinyKeys");

        _backButton = CreateKeyButton(
            "←",
            "后退",
            NativeMethods.VK_LEFT,
            new Rectangle(16, 46, 96, 76),
            _arrowFont,
            "后退：左方向键（短按后退，长按快速回退）");
        _spaceButton = CreateKeyButton(
            "空格",
            "播放 / 暂停",
            NativeMethods.VK_SPACE,
            new Rectangle(120, 46, 96, 76),
            _spaceFont,
            "播放 / 暂停：空格键");
        _forwardButton = CreateKeyButton(
            "→",
            "前进",
            NativeMethods.VK_RIGHT,
            new Rectangle(224, 46, 96, 76),
            _arrowFont,
            "前进：右方向键（短按快进，长按 2 倍速）");

        _volumeZone = new VolumeZone
        {
            Bounds = new Rectangle(336, 46, 88, 76),
            CaptionFont = _volumeFont,
            AccessibleName = "音量，滚动鼠标滚轮调节"
        };

        _hintLabel = new Label
        {
            Bounds = new Rectangle(16, 128, 408, 16),
            Text = _defaultHint,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = _hintFont,
            ForeColor = _defaultHintColor,
            BackColor = Color.White,
            AutoEllipsis = true
        };

        Controls.Add(_hintLabel);
        Controls.Add(_backButton);
        Controls.Add(_spaceButton);
        Controls.Add(_forwardButton);
        Controls.Add(_volumeZone);
        Controls.Add(_pinButton);
        Controls.Add(_minimizeButton);
        Controls.Add(_closeButton);

        _toolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 350,
            ReshowDelay = 100,
            ShowAlways = true
        };
        _toolTip.SetToolTip(_pinButton, "置顶窗口");
        _toolTip.SetToolTip(_minimizeButton, "最小化");
        _toolTip.SetToolTip(_closeButton, "退出");
        _toolTip.SetToolTip(_backButton, "后退：左方向键（短按后退，长按快速回退）");
        _toolTip.SetToolTip(_spaceButton, "播放 / 暂停：空格键");
        _toolTip.SetToolTip(_forwardButton, "前进：右方向键（短按快进，长按 2 倍速）");
        _toolTip.SetToolTip(_volumeZone, "滚动鼠标滚轮调节 Windows 系统音量");

        _pinButton.HeaderClicked += PinButton_Click;
        _minimizeButton.HeaderClicked += MinimizeButton_Click;
        _closeButton.HeaderClicked += CloseButton_Click;

        _backButton.MouseDown += KeyButton_MouseDown;
        _backButton.MouseUp += KeyButton_MouseUp;
        _spaceButton.MouseDown += KeyButton_MouseDown;
        _spaceButton.MouseUp += KeyButton_MouseUp;
        _forwardButton.MouseDown += KeyButton_MouseDown;
        _forwardButton.MouseUp += KeyButton_MouseUp;
        _volumeZone.MouseWheel += VolumeZone_MouseWheel;
        MouseDown += MainForm_MouseDown;

        _holdTimer = new System.Windows.Forms.Timer { Interval = HoldPollIntervalMilliseconds };
        _holdTimer.Tick += HoldTimer_Tick;
        _hintTimer = new System.Windows.Forms.Timer { Interval = 2400 };
        _hintTimer.Tick += HintTimer_Tick;

        _foregroundTracker = new ForegroundTracker(initialTarget);
        StartupTrace.Mark("ForegroundTracker created");

        ResumeLayout(true);
        StartupTrace.Mark("MainForm ctor done");
    }

    private IconButton CreateHeaderButton(HeaderIconKind kind, int x, string accessibleName)
    {
        var button = new IconButton
        {
            Kind = kind,
            Bounds = new Rectangle(x, 8, 26, 26),
            AccessibleName = accessibleName
        };
        return button;
    }

    private KeyButton CreateKeyButton(
        string glyph,
        string caption,
        int virtualKey,
        Rectangle bounds,
        Font glyphFont,
        string accessibleName)
    {
        return new KeyButton
        {
            Glyph = glyph,
            Caption = caption,
            VirtualKey = virtualKey,
            Bounds = bounds,
            GlyphFont = glyphFont,
            CaptionFont = _captionFont,
            AccessibleName = accessibleName
        };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            // Ask Windows for a native shadow around the borderless card.
            parameters.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StartupTrace.Mark("MainForm.OnHandleCreated");
        UpdateRoundedRegion();
        WindowPlacement.PositionAtStartup(Handle, _initialTarget);
        StartupTrace.Mark("Window placement done");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        StartupTrace.Mark("MainForm.OnLoad");
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_firstShownLogged)
        {
            _firstShownLogged = true;
            StartupTrace.Mark("MainForm.OnShown");
        }

        if (IsHandleCreated && !IsDisposed)
        {
            BeginInvoke(new Action(InstallVolumeWheelHook));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        Theme.PrepareGraphics(graphics);
        graphics.Clear(Color.White);

        var scale = ClientSize.Width / (float)DesignWidth;
        using (var borderPath = Theme.RoundedRect(
                   new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f),
                   18f * scale))
        using (var borderPen = new Pen(Theme.CardBorder, 1.2f))
        {
            graphics.DrawPath(borderPen, borderPath);
        }

        using (var titleBrush = new SolidBrush(Theme.TitleText))
        {
            graphics.DrawString("TinyKeys", _titleFont, titleBrush, 16f * scale, 10f * scale);
        }

        using (var dividerPen = new Pen(Theme.Divider, 1f))
        {
            graphics.DrawLine(
                dividerPen,
                328f * scale,
                56f * scale,
                328f * scale,
                112f * scale);
        }

        if (!_firstPaintLogged)
        {
            _firstPaintLogged = true;
            StartupTrace.Mark("MainForm first paint");
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            ReleaseHeldKey();
        }

        UpdateRoundedRegion();
    }

    private void MainForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var scale = ClientSize.Width / (float)DesignWidth;
        if (e.Y > 40f * scale)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(
            Handle,
            NativeMethods.WM_NCLBUTTONDOWN,
            new IntPtr(NativeMethods.HTCAPTION),
            IntPtr.Zero);
    }

    private void UpdateRoundedRegion()
    {
        if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0 || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        var scale = ClientSize.Width / (float)DesignWidth;
        using var path = Theme.RoundedRect(
            new RectangleF(0f, 0f, ClientSize.Width, ClientSize.Height),
            18f * scale);
        var newRegion = new Region(path);
        var oldRegion = Region;
        Region = newRegion;
        oldRegion?.Dispose();
        _roundedRegion = newRegion;
    }

    private void KeyButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && sender is KeyButton button)
        {
            StartHold(button);
        }
    }

    private void KeyButton_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseHeldKey();
        }
    }

    private void StartHold(KeyButton button)
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

        // This is a real hold, not a series of clicks: one key-down now,
        // repeated key-downs at the system keyboard rate while the mouse is
        // held, and one key-up when the mouse is released. Bilibili uses this
        // held state for its native long-press behavior.
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
        _nextRepeatMilliseconds = timings.DelayMilliseconds;
        _holdClock = Stopwatch.StartNew();

        button.IsHeld = true;
        _holdTimer.Start();
    }

    private void HoldTimer_Tick(object? sender, EventArgs e)
    {
        if (!_keyIsDown || _heldButton is null || _holdClock is null)
        {
            ReleaseHeldKey();
            return;
        }

        if (WindowState == FormWindowState.Minimized ||
            !NativeMethods.IsWindow(_heldTarget) ||
            NativeMethods.GetForegroundWindow() != _heldTarget ||
            (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) == 0)
        {
            ReleaseHeldKey();
            return;
        }

        var elapsed = _holdClock.ElapsedMilliseconds;
        if (elapsed < _nextRepeatMilliseconds)
        {
            return;
        }

        if (!KeyboardSender.SendKeyDown(_heldVirtualKey))
        {
            ReleaseHeldKey();
            ShowHint("按键发送失败", isError: true);
            return;
        }

        _repeatCount++;
        _nextRepeatMilliseconds = (int)elapsed + _repeatIntervalMilliseconds;

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
        _holdClock = null;
        _repeatCount = 0;
    }

    private void PinButton_Click(object? sender, EventArgs e)
    {
        _pinned = !_pinned;
        _pinButton.IsChecked = _pinned;
        TopMost = _pinned;
        _toolTip.SetToolTip(_pinButton, _pinned ? "取消置顶" : "置顶窗口");

        if (_pinned && IsHandleCreated)
        {
            NativeMethods.SetWindowPos(
                Handle,
                new IntPtr(-1), // HWND_TOPMOST
                0,
                0,
                0,
                0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void MinimizeButton_Click(object? sender, EventArgs e)
    {
        WindowState = FormWindowState.Minimized;
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void VolumeZone_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_volumeWheelHook?.IsInstalled == true)
        {
            return;
        }

        ApplyVolumeDelta(e.Delta);
    }

    private void InstallVolumeWheelHook()
    {
        if (!IsHandleCreated || IsDisposed || _volumeWheelHook?.IsInstalled == true)
        {
            return;
        }

        StartupTrace.Mark("Installing volume wheel hook");
        _volumeWheelHook = new VolumeWheelHook(HandleVolumeWheel);
        StartupTrace.Mark("Volume wheel hook installed");

        if (!_volumeWheelHook.IsInstalled)
        {
            ShowHint("滚轮音量初始化失败，请重启 TinyKeys", isError: true);
        }
    }

    private bool HandleVolumeWheel(int screenX, int screenY, int delta)
    {
        if (!IsHandleCreated ||
            !Visible ||
            WindowState == FormWindowState.Minimized)
        {
            return false;
        }

        var screenBounds = _volumeZone.RectangleToScreen(_volumeZone.ClientRectangle);
        if (!screenBounds.Contains(screenX, screenY))
        {
            return false;
        }

        var hit = NativeMethods.WindowFromPoint(new NativeMethods.POINT(screenX, screenY));
        if (hit == IntPtr.Zero || NativeMethods.GetAncestor(hit, NativeMethods.GA_ROOT) != Handle)
        {
            return false;
        }

        ApplyVolumeDelta(delta);
        return true;
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

    private void ShowHint(string message, bool isError)
    {
        _hintLabel.Text = message;
        _hintLabel.ForeColor = isError ? Theme.ErrorText : _defaultHintColor;
        _hintTimer.Stop();
        _hintTimer.Start();
    }

    private void HintTimer_Tick(object? sender, EventArgs e)
    {
        _hintTimer.Stop();
        _hintLabel.Text = _defaultHint;
        _hintLabel.ForeColor = _defaultHintColor;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ReleaseHeldKey();
        _holdTimer.Stop();
        _holdTimer.Dispose();
        _hintTimer.Stop();
        _hintTimer.Dispose();
        _volumeWheelHook?.Dispose();
        _volumeWheelHook = null;
        _foregroundTracker.Dispose();
        _toolTip.Dispose();

        var region = _roundedRegion;
        _roundedRegion = null;
        Region = null;
        region?.Dispose();

        base.OnFormClosed(e);
    }
}
