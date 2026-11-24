using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TinyKeys.Ui;

internal sealed class VolumeZone : Control
{
    private bool _hovered;

    public VolumeZone()
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
        AccessibleRole = AccessibleRole.Grouping;
    }

    public Font? CaptionFont { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        Theme.PrepareGraphics(graphics);
        graphics.Clear(Color.White);

        var scale = Height / 76f;
        var outer = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (var path = Theme.RoundedRect(outer, 14f * scale))
        using (var background = new SolidBrush(_hovered ? Theme.VolumeHover : Theme.VolumeBackground))
        using (var border = new Pen(Theme.VolumeBorder, 1f))
        {
            graphics.FillPath(background, path);
            graphics.DrawPath(border, path);
        }

        var circleSize = 34f * scale;
        var circle = new RectangleF(
            (Width - circleSize) / 2f,
            10f * scale,
            circleSize,
            circleSize);
        using (var circleBrush = new SolidBrush(Theme.VolumeIconBackground))
        {
            graphics.FillEllipse(circleBrush, circle);
        }

        DrawSpeaker(graphics, scale);

        var font = CaptionFont ?? Font;
        using var captionBrush = new SolidBrush(Theme.TextSecondary);
        var captionRect = new RectangleF(0f, 50f * scale, Width, 16f * scale);
        Theme.DrawCentered(graphics, "音量", font, captionBrush, captionRect);
    }

    private static void DrawSpeaker(Graphics graphics, float scale)
    {
        const float designSize = 24f;
        var targetSize = 18f * scale;
        var offsetX = (float)Math.Round((graphics.VisibleClipBounds.Width - targetSize) / 2f);
        var offsetY = (float)Math.Round(10f * scale + (34f * scale - targetSize) / 2f);
        var state = graphics.Save();
        graphics.TranslateTransform(offsetX, offsetY);
        graphics.ScaleTransform(targetSize / designSize, targetSize / designSize);

        using (var body = new GraphicsPath())
        {
            body.AddPolygon(new[]
            {
                new PointF(3f, 9f),
                new PointF(7f, 9f),
                new PointF(12f, 4f),
                new PointF(12f, 20f),
                new PointF(7f, 15f),
                new PointF(3f, 15f)
            });
            using var bodyBrush = new SolidBrush(Theme.Accent);
            graphics.FillPath(bodyBrush, body);
        }

        using (var wavePen = new Pen(Theme.Accent, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            graphics.DrawBezier(wavePen, 14.5f, 9f, 17f, 11f, 17f, 13f, 14.5f, 15f);
        }

        using (var wavePen = new Pen(Color.FromArgb(166, Theme.Accent.R, Theme.Accent.G, Theme.Accent.B), 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            graphics.DrawBezier(wavePen, 17.5f, 6.5f, 21.5f, 11f, 21.5f, 13f, 17.5f, 17.5f);
        }

        graphics.Restore(state);
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
