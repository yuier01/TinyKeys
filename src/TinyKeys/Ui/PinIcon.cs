using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TinyKeys.Ui;

/// <summary>
/// Converts the Fluent UI pin SVG path into a GDI+ path so the WinForms
/// version can keep the same crisp vector pin icon as the previous UI.
/// The path data is from Microsoft Fluent UI System Icons (MIT).
/// </summary>
internal static class PinIcon
{
    private const string PathData =
        "M16.2425 2.93189 L21.0682 7.75765 " +
        "C22.3955 9.08491 22.0324 11.3224 20.3535 12.1619 " +
        "L15.4826 14.5973 C15.3073 14.685 15.1732 14.8379 15.1092 15.0232 " +
        "L13.6699 19.1895 C13.3684 20.0622 12.2574 20.3181 11.6045 19.6653 " +
        "L8.50002 16.5607 L4.06074 21.0001 H3 L3.00008 19.9394 " +
        "L7.43936 15.5001 L4.33487 12.3956 " +
        "C3.682 11.7427 3.93791 10.6317 4.81061 10.3302 " +
        "L8.97688 8.89096 C9.16223 8.82694 9.31512 8.69287 9.40281 8.51748 " +
        "L11.8382 3.6466 C12.6777 1.96772 14.9152 1.60462 16.2425 2.93189 Z " +
        "M20.0076 8.81831 L15.1818 3.99255 " +
        "C14.5785 3.38924 13.5614 3.55429 13.1799 4.31742 " +
        "L10.7445 9.18829 C10.4814 9.71446 10.0227 10.1167 9.46666 10.3087 " +
        "L5.67812 11.6175 L12.3826 18.322 L13.6914 14.5335 " +
        "C13.8835 13.9774 14.2857 13.5188 14.8118 13.2557 " +
        "L19.6827 10.8202 C20.4458 10.4387 20.6109 9.42161 20.0076 8.81831 Z";

    private static readonly Lazy<GraphicsPath> BasePath = new(BuildBasePath);

    public static GraphicsPath CreatePath(RectangleF target)
    {
        var source = BasePath.Value;
        var bounds = source.GetBounds();
        var scale = Math.Min(target.Width / bounds.Width, target.Height / bounds.Height);
        var offsetX = target.Left + (target.Width - bounds.Width * scale) / 2f - bounds.Left * scale;
        var offsetY = target.Top + (target.Height - bounds.Height * scale) / 2f - bounds.Top * scale;

        var result = (GraphicsPath)source.Clone();
        using var matrix = new Matrix(scale, 0f, 0f, scale, offsetX, offsetY);
        result.Transform(matrix);
        return result;
    }

    private static GraphicsPath BuildBasePath()
    {
        var path = new GraphicsPath(FillMode.Winding);
        var tokens = Regex.Matches(PathData, @"[MLCHVZ]|-?\d*\.?\d+")
            .Cast<Match>()
            .Select(match => match.Value)
            .ToArray();

        var index = 0;
        var command = 'M';
        var current = PointF.Empty;
        var figureStart = PointF.Empty;

        float NextNumber()
        {
            return float.Parse(tokens[index++], CultureInfo.InvariantCulture);
        }

        path.StartFigure();
        while (index < tokens.Length)
        {
            var token = tokens[index];
            if (char.IsLetter(token[0]))
            {
                command = char.ToUpperInvariant(token[0]);
                index++;
                if (command == 'Z')
                {
                    path.CloseFigure();
                    current = figureStart;
                    continue;
                }
            }

            switch (command)
            {
                case 'M':
                {
                    var point = new PointF(NextNumber(), NextNumber());
                    path.StartFigure();
                    current = point;
                    figureStart = point;
                    command = 'L';
                    break;
                }
                case 'L':
                {
                    var point = new PointF(NextNumber(), NextNumber());
                    path.AddLine(current, point);
                    current = point;
                    break;
                }
                case 'H':
                {
                    var x = NextNumber();
                    var point = new PointF(x, current.Y);
                    path.AddLine(current, point);
                    current = point;
                    break;
                }
                case 'V':
                {
                    var y = NextNumber();
                    var point = new PointF(current.X, y);
                    path.AddLine(current, point);
                    current = point;
                    break;
                }
                case 'C':
                {
                    var c1 = new PointF(NextNumber(), NextNumber());
                    var c2 = new PointF(NextNumber(), NextNumber());
                    var end = new PointF(NextNumber(), NextNumber());
                    path.AddBezier(current, c1, c2, end);
                    current = end;
                    break;
                }
            }
        }

        path.FillMode = FillMode.Winding;
        return path;
    }
}
