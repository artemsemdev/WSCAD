using FluentAssertions;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Rendering.Renderers;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// Renderers are tested through the <see cref="DrawCommand"/> model rather than by drawing
/// pixels. "A filled circle produces a border and a fill" becomes a plain assertion, and no
/// UI framework is involved.
/// </summary>
public class PrimitiveRendererTests
{
    private static readonly ArgbColor White = new(127, 255, 255, 255);
    private static readonly ArgbColor Red = new(127, 255, 0, 0);
    private static readonly ArgbColor Magenta = new(127, 255, 0, 255);

    /// <summary>Scale 2, world origin pinned to pixel (100, 100) — chosen so expected values are obvious.</summary>
    private static RenderContext Context() =>
        new(new ViewportTransform(2, Point2D.Origin, new ScreenPoint(100, 100)), RenderOptions.Default);

    private static List<DrawCommand> Render(IPrimitiveRenderer renderer, IPrimitive primitive)
    {
        var output = new List<DrawCommand>();
        renderer.Render(primitive, Context(), output);
        return output;
    }

    public class Lines
    {
        [Fact]
        public void A_line_becomes_a_single_line_command_with_transformed_endpoints()
        {
            var line = new Line(new Point2D(-1.5, 3.4), new Point2D(2.2, 5.7), White);

            var command = Render(new LineRenderer(), line).Should().ContainSingle()
                .Which.Should().BeOfType<DrawLine>().Subject;

            command.Start.ShouldBe(97, 93.2);
            command.End.ShouldBe(104.4, 88.6);
        }

        [Fact]
        public void A_line_is_stroked_in_its_own_colour_and_never_filled()
        {
            var line = new Line(Point2D.Origin, new Point2D(10, 10), White);

            var command = (DrawLine)Render(new LineRenderer(), line).Single();

            command.Appearance.Stroke!.Value.Color.Should().Be(White);
            command.Appearance.Stroke!.Value.Thickness.Should().Be(2);
            command.Appearance.Fill.Should().BeNull();
        }
    }

    public class Circles
    {
        [Fact]
        public void A_circle_becomes_an_ellipse_with_equal_radii_because_the_scale_is_uniform()
        {
            var circle = new Circle(Point2D.Origin, 15, Red, Filled: false);

            var command = Render(new CircleRenderer(), circle).Should().ContainSingle()
                .Which.Should().BeOfType<DrawEllipse>().Subject;

            command.Center.ShouldBe(100, 100);
            command.RadiusX.Should().Be(30);
            command.RadiusY.Should().Be(30);
        }

        [Fact]
        public void An_unfilled_circle_is_drawn_as_a_border_only()
        {
            var circle = new Circle(Point2D.Origin, 15, Red, Filled: false);

            var command = (DrawEllipse)Render(new CircleRenderer(), circle).Single();

            command.Appearance.Stroke!.Value.Color.Should().Be(Red);
            command.Appearance.Fill.Should().BeNull();
        }

        [Fact]
        public void A_filled_circle_is_drawn_with_a_border_and_a_fill()
        {
            var circle = new Circle(new Point2D(4, -2), 3, Red, Filled: true);

            var command = (DrawEllipse)Render(new CircleRenderer(), circle).Single();

            command.Center.ShouldBe(108, 104);
            command.Appearance.Stroke!.Value.Color.Should().Be(Red);
            command.Appearance.Fill.Should().Be(Red);
        }
    }

    public class Triangles
    {
        private static readonly Triangle Sample = new(
            new Point2D(-15, -20), new Point2D(15, -20.3), new Point2D(0, 21), Magenta, Filled: true);

        [Fact]
        public void A_triangle_becomes_a_polygon_of_its_three_transformed_vertices_in_order()
        {
            var command = Render(new TriangleRenderer(), Sample).Should().ContainSingle()
                .Which.Should().BeOfType<DrawPolygon>().Subject;

            command.Points.Should().HaveCount(3);
            command.Points[0].ShouldBe(70, 140);
            command.Points[1].ShouldBe(130, 140.6);
            command.Points[2].ShouldBe(100, 58);
        }

        [Fact]
        public void A_filled_triangle_carries_a_fill()
        {
            var command = (DrawPolygon)Render(new TriangleRenderer(), Sample).Single();

            command.Appearance.Fill.Should().Be(Magenta);
            command.Appearance.Stroke!.Value.Color.Should().Be(Magenta);
        }

        [Fact]
        public void An_unfilled_triangle_carries_a_border_only()
        {
            var command = (DrawPolygon)Render(new TriangleRenderer(), Sample with { Filled = false }).Single();

            command.Appearance.Fill.Should().BeNull();
            command.Appearance.Stroke.Should().NotBeNull();
        }
    }

    public class Dispatch
    {
        [Fact]
        public void A_renderer_rejects_a_primitive_it_does_not_handle()
        {
            var act = () => Render(new CircleRenderer(), new Line(Point2D.Origin, new Point2D(1, 1), White));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void A_renderer_reports_the_primitive_type_it_handles()
        {
            new LineRenderer().PrimitiveType.Should().Be<Line>();
            new CircleRenderer().PrimitiveType.Should().Be<Circle>();
            new TriangleRenderer().PrimitiveType.Should().Be<Triangle>();
        }
    }
}
