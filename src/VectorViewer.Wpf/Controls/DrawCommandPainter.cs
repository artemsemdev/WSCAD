using System.Windows;
using System.Windows.Media;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;

namespace VectorViewer.Wpf.Controls;

/// <summary>
/// Paints draw commands onto a WPF <see cref="DrawingContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the only type in the solution that touches a graphics API. It switches over the
/// <see cref="DrawCommand"/> vocabulary — three cases — rather than over primitive types, which
/// is why adding a primitive never reaches the UI: a new primitive reuses these shapes.
/// </para>
/// <para>
/// Brushes and pens are cached per colour and frozen. A redraw happens on every resize, and
/// creating a fresh <see cref="Brush"/> per shape would allocate and re-register change
/// notification for objects that are never mutated.
/// </para>
/// </remarks>
public sealed class DrawCommandPainter
{
    private readonly Dictionary<uint, Brush> _brushes = [];
    private readonly Dictionary<(uint Color, double Thickness), Pen> _pens = [];

    public void Paint(DrawingContext context, IReadOnlyList<DrawCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(commands);

        foreach (var command in commands)
        {
            var pen = command.Appearance.Stroke is { } stroke ? GetPen(stroke) : null;
            var fill = command.Appearance.Fill is { } fillColor ? GetBrush(fillColor) : null;

            switch (command)
            {
                case DrawLine line:
                    // A line has no interior, so only the pen applies.
                    context.DrawLine(pen, ToPoint(line.Start), ToPoint(line.End));
                    break;

                case DrawEllipse ellipse:
                    context.DrawEllipse(fill, pen, ToPoint(ellipse.Center), ellipse.RadiusX, ellipse.RadiusY);
                    break;

                case DrawPolygon polygon:
                    context.DrawGeometry(fill, pen, CreatePolygonGeometry(polygon.Points));
                    break;

                default:
                    throw new NotSupportedException(
                        $"No painter case for draw command '{command.GetType().Name}'.");
            }
        }
    }

    private static Point ToPoint(ScreenPoint point) => new(point.X, point.Y);

    private static Geometry CreatePolygonGeometry(IReadOnlyList<ScreenPoint> points)
    {
        var figure = new PathFigure { StartPoint = ToPoint(points[0]), IsClosed = true, IsFilled = true };
        for (var i = 1; i < points.Count; i++)
        {
            figure.Segments.Add(new LineSegment(ToPoint(points[i]), isStroked: true));
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    private Brush GetBrush(ArgbColor color)
    {
        var key = color.ToUInt32();
        if (!_brushes.TryGetValue(key, out var brush))
        {
            brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            brush.Freeze();
            _brushes[key] = brush;
        }

        return brush;
    }

    private Pen GetPen(Stroke stroke)
    {
        var key = (stroke.Color.ToUInt32(), stroke.Thickness);
        if (!_pens.TryGetValue(key, out var pen))
        {
            pen = new Pen(GetBrush(stroke.Color), stroke.Thickness)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
            };
            pen.Freeze();
            _pens[key] = pen;
        }

        return pen;
    }
}
