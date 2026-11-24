using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TinyKeys.Ui;

internal enum HeaderIconKind
{
    Pin,
    Minimize,
    Close
}

internal sealed class IconButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _isChecked;

    public IconButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.White;
        TabStop = false;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        Size = new Size(26, 26);
    }

    public HeaderIconKind Kind { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            Invalidate();
        }
    }

    public event EventHandler? HeaderClicked;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        Theme.PrepareGraphics(graphics);
        graphics.Clear(Color.White);

        var scale = Math.Min(Width, Height) / 26f;
        var surface = new RectangleF(1f * scale, 1f * scale, Width - 2f * scale, Height - 2f * scale);

        Color? background = null;
        if (_isChecked)
        {
            background = Theme.PinActiveBackground;
        }
        else if (_pressed)
        {
            background = Theme.HeaderPressed;
        }
        else if (_hovered)
        {
            background = Theme.HeaderHover;
        }

        if (background.HasValue)
        {
            using var path = Theme.RoundedRect(surface, 7f * scale);
            using var brush = new SolidBrush(background.Value);
            graphics.FillPath(brush, path);
        }

        switch (Kind)
        {
            case HeaderIconKind.Pin:
                DrawPin(graphics, scale);
                break;
            case HeaderIconKind.Minimize:
                DrawMinimize(graphics, scale);
                break;
            case HeaderIconKind.Close:
                DrawClose(graphics, scale);
                break;
        }
    }

    private void DrawPin(Graphics graphics, float scale)
    {
        var side = 14f * scale;
        var target = new RectangleF(
            (Width - side) / 2f,
            (Height - side) / 2f,
            side,
            side);
        var color = _isChecked ? Theme.AccentDeep : Theme.HeaderIcon;
        using var path = PinIcon.CreatePath(target);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    private void DrawMinimize(Graphics graphics, float scale)
    {
        var color = _isChecked ? Theme.AccentDeep : Theme.HeaderIcon;
        using var pen = new Pen(color, 1.6f * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var half = 5f * scale;
        var y = Height / 2f;
        graphics.DrawLine(pen, Width / 2f - half, y, Width / 2f + half, y);
    }

    private void DrawClose(Graphics graphics, float scale)
    {
        using var pen = new Pen(Theme.HeaderIcon, 1.6f * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var half = 4.5f * scale;
        var cx = Width / 2f;
        var cy = Height / 2f;
        graphics.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
        graphics.DrawLine(pen, cx + half, cy - half, cx - half, cy + half);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Capture = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var shouldClick = _pressed && ClientRectangle.Contains(e.Location);
            _pressed = false;
            Capture = false;
            Invalidate();

            if (shouldClick)
            {
                HeaderClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        base.OnMouseUp(e);
    }
}
