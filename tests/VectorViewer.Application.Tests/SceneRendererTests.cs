using FluentAssertions;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// End-to-end behaviour of a redraw: scene bounds → fit transform → draw commands.
/// </summary>
public class SceneRendererTests
{
    private static readonly ArgbColor Red = new(127, 255, 0, 0);
    private static readonly ArgbColor White = new(127, 255, 255, 255);

    private static SceneRenderer Renderer() => new(PrimitiveRendererRegistry.CreateDefault());

    [Fact]
    public void Every_primitive_produces_commands_in_draw_order()
    {
        var scene = new Scene([
            new Circle(Point2D.Origin, 15, Red, Filled: false),
            new Line(new Point2D(-1.5, 3.4), new Point2D(2.2, 5.7), White),
            new Triangle(new Point2D(-15, -20), new Point2D(15, -20.3), new Point2D(0, 21), Red, Filled: true),
        ]);

        var rendered = Renderer().Render(scene, new ViewportSize(800, 600));

        rendered.Commands.Should().HaveCount(3);
        rendered.Commands[0].Should().BeOfType<DrawEllipse>();
        rendered.Commands[1].Should().BeOfType<DrawLine>();
        rendered.Commands[2].Should().BeOfType<DrawPolygon>();
    }

    [Fact]
    public void The_transform_is_derived_from_the_scene_bounds()
    {
        // A circle of radius 250 spans 500 units; in a 250x250 viewport (no padding) that is
        // exactly half scale. This also proves circle bounds take part in the fit calculation.
        var scene = new Scene([new Circle(Point2D.Origin, 250, Red, Filled: false)]);

        var rendered = Renderer().Render(scene, new ViewportSize(250, 250), new ViewportFitOptions(Padding: 0));

        rendered.Scale.Should().Be(0.5);
    }

    [Fact]
    public void A_circle_is_scaled_to_stay_inside_the_viewport()
    {
        var scene = new Scene([new Circle(Point2D.Origin, 250, Red, Filled: false)]);

        var rendered = Renderer().Render(scene, new ViewportSize(250, 250), new ViewportFitOptions(Padding: 0));
        var ellipse = (DrawEllipse)rendered.Commands.Single();

        ellipse.Center.ShouldBe(125, 125);
        ellipse.RadiusX.Should().Be(125);
        (ellipse.Center.X - ellipse.RadiusX).Should().BeGreaterThanOrEqualTo(0);
        (ellipse.Center.X + ellipse.RadiusX).Should().BeLessThanOrEqualTo(250);
    }

    [Fact]
    public void A_small_drawing_is_rendered_at_100_percent()
    {
        var scene = new Scene([new Line(new Point2D(0, 0), new Point2D(10, 0), White)]);

        var rendered = Renderer().Render(scene, new ViewportSize(800, 600));

        rendered.Scale.Should().Be(1.0);
        var line = (DrawLine)rendered.Commands.Single();
        (line.End.X - line.Start.X).Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void An_empty_scene_renders_nothing_without_throwing()
    {
        var rendered = Renderer().Render(Scene.Empty, new ViewportSize(800, 600));

        rendered.Commands.Should().BeEmpty();
        rendered.Scale.Should().Be(1.0);
    }

    [Fact]
    public void An_empty_viewport_renders_without_throwing()
    {
        // WPF raises a layout pass with zero size before the window is shown.
        var scene = new Scene([new Circle(Point2D.Origin, 15, Red, Filled: false)]);

        var act = () => Renderer().Render(scene, new ViewportSize(0, 0));

        act.Should().NotThrow();
    }

    [Fact]
    public void A_primitive_with_no_registered_renderer_fails_loudly_and_names_the_type()
    {
        var scene = new Scene([new UnregisteredPrimitive()]);

        var act = () => Renderer().Render(scene, new ViewportSize(800, 600));

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*UnregisteredPrimitive*",
                "silently dropping a shape would be worse than failing with a clear message");
    }

    [Fact]
    public void Rendering_the_same_scene_twice_gives_the_same_result()
    {
        // Redraw happens on every resize; it must be a pure function of scene and viewport.
        var scene = new Scene([new Circle(new Point2D(3, -4), 15, Red, Filled: true)]);
        var renderer = Renderer();

        var first = renderer.Render(scene, new ViewportSize(640, 480));
        var second = renderer.Render(scene, new ViewportSize(640, 480));

        second.Transform.Should().Be(first.Transform);
        second.Commands.Should().BeEquivalentTo(first.Commands);
    }

    private sealed record UnregisteredPrimitive : IPrimitive
    {
        public ArgbColor Color => new(255, 0, 0, 0);

        public BoundingBox Bounds => new(0, 0, 1, 1);
    }
}
