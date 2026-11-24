using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace TinyKeys.Ui;

internal static class Theme
{
    public static readonly Color Card = Color.White;
    public static readonly Color CardBorder = Color.FromArgb(231, 233, 239);

    public static readonly Color TextPrimary = Color.FromArgb(32, 36, 43);
    public static readonly Color TextSecondary = Color.FromArgb(131, 139, 151);
    public static readonly Color TextMuted = Color.FromArgb(154, 161, 172);
    public static readonly Color TitleText = Color.FromArgb(93, 102, 115);
    public static readonly Color ErrorText = Color.FromArgb(194, 74, 90);

    public static readonly Color Accent = Color.FromArgb(251, 114, 153);
    public static readonly Color AccentDeep = Color.FromArgb(231, 95, 135);

    public static readonly Color KeyBackground = Color.FromArgb(248, 249, 251);
    public static readonly Color KeyBorder = Color.FromArgb(232, 235, 240);
    public static readonly Color KeyEdge = Color.FromArgb(231, 234, 240);
    public static readonly Color KeyHover = Color.FromArgb(241, 244, 248);
    public static readonly Color KeyHoverEdge = Color.FromArgb(224, 228, 234);
    public static readonly Color KeyHeld = Color.FromArgb(233, 237, 243);
    public static readonly Color KeyHeldEdge = Color.FromArgb(221, 226, 233);

    public static readonly Color VolumeBackground = Color.FromArgb(255, 246, 248);
    public static readonly Color VolumeHover = Color.FromArgb(255, 240, 244);
    public static readonly Color VolumeBorder = Color.FromArgb(251, 231, 237);
    public static readonly Color VolumeIconBackground = Color.FromArgb(255, 232, 239);

    public static readonly Color PinActiveBackground = Color.FromArgb(255, 240, 244);
    public static readonly Color HeaderHover = Color.FromArgb(242, 244, 247);
    public static readonly Color HeaderPressed = Color.FromArgb(233, 237, 243);
    public static readonly Color HeaderIcon = Color.FromArgb(138, 145, 156);
    public static readonly Color Divider = Color.FromArgb(236, 238, 242);

    public const string UiFontFamily = "Microsoft YaHei UI";
    public const string SymbolFontFamily = "Segoe UI Symbol";

    public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0.5f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    public static void PrepareGraphics(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static void DrawCentered(
        Graphics graphics,
        string text,
        Font font,
        Brush brush,
        RectangleF bounds)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }
}
