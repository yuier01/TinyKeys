using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TinyKeys.Ui;

internal sealed class KeyButton : Control
{
    private bool _hovered;
    private bool _isHeld;

    public KeyButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.White;
        TabStop = false;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
    }

    public int VirtualKey { get; set; }

    public string Glyph { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public Font? GlyphFont { get; set; }

    public Font? CaptionFont { get; set; }

    public bool IsHeld
    {
        get => _isHeld;
        set
        {
            if (_isHeld == value)
            {
                return;
            }

            _isHeld = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        Theme.PrepareGraphics(graphics);
        graphics.Clear(Color.White);

        var scale = Height / 76f;
        var background = _isHeld ? Theme.KeyHeld : (_hovered ? Theme.KeyHover : Theme.KeyBackground);
        var edge = _isHeld ? Theme.KeyHeldEdge : (_hovered ? Theme.KeyHoverEdge : Theme.KeyEdge);

        var edgeRect = new RectangleF(0.5f, 2.5f * scale, Width - 1f, Height - 3.5f * scale);
        using (var edgePath = Theme.RoundedRect(edgeRect, 14f * scale))
        using (var edgeBrush = new SolidBrush(edge))
        {
            graphics.FillPath(edgeBrush, edgePath);
        }

        var keyRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 3f * scale);
        using (var keyPath = Theme.RoundedRect(keyRect, 14f * scale))
        using (var keyBrush = new SolidBrush(background))
        using (var borderPen = new Pen(Theme.KeyBorder, 1f))
        {
            graphics.FillPath(keyBrush, keyPath);
            graphics.DrawPath(borderPen, keyPath);
        }

        var glyphFont = GlyphFont ?? Font;
        var captionFont = CaptionFont ?? Font;
        var glyphRect = new RectangleF(0f, Height * 0.11f, Width, Height * 0.50f);
        var captionRect = new RectangleF(0f, Height * 0.61f, Width, Height * 0.24f);

        using (var glyphBrush = new SolidBrush(Theme.TextPrimary))
        using (var captionBrush = new SolidBrush(Theme.TextSecondary))
        {
            Theme.DrawCentered(graphics, Glyph, glyphFont, glyphBrush, glyphRect);
            Theme.DrawCentered(graphics, Caption, captionFont, captionBrush, captionRect);
        }
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
        Invalidate();
        base.OnMouseLeave(e);
    }
}
