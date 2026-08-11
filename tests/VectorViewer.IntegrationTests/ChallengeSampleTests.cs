using FluentAssertions;
using VectorViewer.Application.Documents;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using VectorViewer.Infrastructure.Json;
using Xunit;

namespace VectorViewer.IntegrationTests;

/// <summary>
/// Drives the exact payload from the challenge document through the whole pipeline:
/// file → reader → scene → bounds → fit transform → draw commands.
/// </summary>
public class ChallengeSampleTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "example.json");

    private static Scene LoadSample() =>
        new VectorDocumentLoader([new JsonVectorDocumentReader()]).Load(SamplePath);

    [Fact]
    public void The_sample_file_ships_with_the_tests()
    {
        File.Exists(SamplePath).Should().BeTrue($"the fixture is expected at {SamplePath}");
    }

    [Fact]
    public void The_sample_contains_one_line_one_circle_and_one_triangle_in_order()
    {
        var scene = LoadSample();

        scene.Primitives.Select(p => p.GetType())
            .Should().Equal(typeof(Line), typeof(Circle), typeof(Triangle));
    }

    [Fact]
    public void Every_value_of_the_sample_is_read_exactly()
    {
        var scene = LoadSample();

        var line = (Line)scene.Primitives[0];
        line.Start.Should().Be(new Point2D(-1.5, 3.4));
        line.End.Should().Be(new Point2D(2.2, 5.7));
        line.Color.Should().Be(new ArgbColor(127, 255, 255, 255));

        var circle = (Circle)scene.Primitives[1];
        circle.Center.Should().Be(Point2D.Origin);
        circle.Radius.Should().Be(15.0);
        circle.Filled.Should().BeFalse();
        circle.Color.Should().Be(new ArgbColor(127, 255, 0, 0));

        var triangle = (Triangle)scene.Primitives[2];
        triangle.A.Should().Be(new Point2D(-15, -20));
        triangle.B.Should().Be(new Point2D(15, -20.3));
        triangle.C.Should().Be(new Point2D(0, 21));
        triangle.Filled.Should().BeTrue();
        triangle.Color.Should().Be(new ArgbColor(127, 255, 0, 255));
    }

    [Fact]
    public void The_scene_bounds_span_the_triangle_and_the_circle()
    {
        // X: the circle and the triangle both reach ±15. Y: the triangle's base (-20.3) and
        // its apex (21) are the extremes; the circle only reaches ±15.
        LoadSample().Bounds.Should().Be(new BoundingBox(-15, -20.3, 15, 21));
    }

    [Fact]
    public void The_sample_is_drawn_at_100_percent_in_a_normal_window()
    {
        // The drawing is ~30x41 units, far smaller than a window, so it must not be magnified.
        var rendered = Render(new ViewportSize(800, 600));

        rendered.Scale.Should().Be(1.0);
    }

    [Fact]
    public void The_sample_is_scaled_down_to_fit_a_small_window()
    {
        // The drawing is 30 units wide; 15 pixels of usable width ⇒ exactly half scale,
        // and width is the binding constraint since 1000px easily covers the 41.3-unit height.
        var rendered = Render(new ViewportSize(15, 1000), new ViewportFitOptions(Padding: 0));

        rendered.Scale.Should().Be(0.5);
    }

    [Fact]
    public void Every_primitive_of_the_sample_produces_the_expected_kind_of_command()
    {
        var commands = Render(new ViewportSize(800, 600)).Commands;

        commands.Should().HaveCount(3);
        commands[0].Should().BeOfType<DrawLine>();
        commands[1].Should().BeOfType<DrawEllipse>();
        commands[2].Should().BeOfType<DrawPolygon>();
    }

    [Fact]
    public void The_unfilled_circle_is_drawn_as_a_border_only_and_the_filled_triangle_with_a_fill()
    {
        var commands = Render(new ViewportSize(800, 600)).Commands;

        var circle = (DrawEllipse)commands[1];
        circle.Appearance.Fill.Should().BeNull("the sample circle has filled: false");
        circle.Appearance.Stroke!.Value.Color.Should().Be(new ArgbColor(127, 255, 0, 0));

        var triangle = (DrawPolygon)commands[2];
        triangle.Appearance.Fill.Should().Be(new ArgbColor(127, 255, 0, 255));
        triangle.Appearance.Stroke.Should().NotBeNull();
    }

    [Fact]
    public void The_whole_drawing_lands_inside_the_viewport_and_is_centred()
    {
        var viewport = new ViewportSize(400, 300);
        var rendered = Render(viewport);

        // Scene bounds (-15, -20.3)-(15, 21) at 100 %: 30 wide, 41.3 tall, centred on (0, 0.35).
        rendered.Transform.ToScreen(new Point2D(0, 0.35)).X.Should().BeApproximately(200, 1e-9);
        rendered.Transform.ToScreen(new Point2D(0, 0.35)).Y.Should().BeApproximately(150, 1e-9);

        foreach (var point in AllScreenPoints(rendered.Commands))
        {
            point.X.Should().BeInRange(0, viewport.Width);
            point.Y.Should().BeInRange(0, viewport.Height);
        }
    }

    [Fact]
    public void The_y_axis_points_up_the_triangle_apex_is_drawn_above_its_base()
    {
        var polygon = (DrawPolygon)Render(new ViewportSize(800, 600)).Commands[2];

        // Apex (0, 21) is the third vertex; the base vertices sit at y = -20 and -20,3.
        polygon.Points[2].Y.Should().BeLessThan(polygon.Points[0].Y);
        polygon.Points[2].Y.Should().BeLessThan(polygon.Points[1].Y);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(320, 240)]
    [InlineData(100, 900)]
    [InlineData(900, 100)]
    [InlineData(30, 30)]
    public void The_drawing_fits_any_window_size(double width, double height)
    {
        var rendered = Render(new ViewportSize(width, height));

        rendered.Scale.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(1.0);
        foreach (var point in AllScreenPoints(rendered.Commands))
        {
            point.X.Should().BeInRange(0, width);
            point.Y.Should().BeInRange(0, height);
        }
    }

    [Fact]
    public void Reloading_the_same_file_produces_an_equal_scene()
    {
        // Parsing is deterministic — the viewer parses once per file, never per redraw.
        LoadSample().Primitives.Should().BeEquivalentTo(LoadSample().Primitives);
    }

    private static RenderedScene Render(ViewportSize viewport, ViewportFitOptions? options = null) =>
        new SceneRenderer(PrimitiveRendererRegistry.CreateDefault()).Render(LoadSample(), viewport, options);

    /// <summary>Every screen coordinate touched by the drawing, ignoring stroke width.</summary>
    private static IEnumerable<ScreenPoint> AllScreenPoints(IEnumerable<DrawCommand> commands)
    {
        foreach (var command in commands)
        {
            switch (command)
            {
                case DrawLine line:
                    yield return line.Start;
                    yield return line.End;
                    break;
                case DrawEllipse ellipse:
                    yield return new ScreenPoint(ellipse.Center.X - ellipse.RadiusX, ellipse.Center.Y - ellipse.RadiusY);
                    yield return new ScreenPoint(ellipse.Center.X + ellipse.RadiusX, ellipse.Center.Y + ellipse.RadiusY);
                    break;
                case DrawPolygon polygon:
                    foreach (var point in polygon.Points)
                    {
                        yield return point;
                    }

                    break;
            }
        }
    }
}
