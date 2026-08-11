using VectorViewer.Application.Viewport;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Application.Rendering.Renderers;

/// <summary>Draws a line as a single stroked segment.</summary>
public sealed class LineRenderer : PrimitiveRenderer<Line>
{
    protected override void Render(Line line, RenderContext context, ICollection<DrawCommand> output) =>
        output.Add(new DrawLine(
            context.ToScreen(line.Start),
            context.ToScreen(line.End),
            context.AppearanceFor(line)));
}

/// <summary>
/// Draws a circle as an ellipse. Both radii are equal because the viewport scale is uniform —
/// a circle stays a circle at any zoom level.
/// </summary>
public sealed class CircleRenderer : PrimitiveRenderer<Circle>
{
    protected override void Render(Circle circle, RenderContext context, ICollection<DrawCommand> output)
    {
        var radius = context.ToScreenLength(circle.Radius);
        output.Add(new DrawEllipse(
            context.ToScreen(circle.Center),
            radius,
            radius,
            context.AppearanceFor(circle)));
    }
}

/// <summary>Draws a triangle as a closed three-point polygon.</summary>
public sealed class TriangleRenderer : PrimitiveRenderer<Triangle>
{
    protected override void Render(Triangle triangle, RenderContext context, ICollection<DrawCommand> output) =>
        output.Add(new DrawPolygon(
            new ScreenPoint[]
            {
                context.ToScreen(triangle.A),
                context.ToScreen(triangle.B),
                context.ToScreen(triangle.C),
            },
            context.AppearanceFor(triangle)));
}
