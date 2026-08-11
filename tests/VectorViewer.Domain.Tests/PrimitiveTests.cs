using FluentAssertions;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Domain.Tests;

public class LineTests
{
    private static readonly ArgbColor White = new(127, 255, 255, 255);

    [Fact]
    public void Stores_its_endpoints()
    {
        var line = new Line(new Point2D(-1.5, 3.4), new Point2D(2.2, 5.7), White);

        line.Start.Should().Be(new Point2D(-1.5, 3.4));
        line.End.Should().Be(new Point2D(2.2, 5.7));
        line.Color.Should().Be(White);
    }

    [Fact]
    public void Bounds_span_both_endpoints()
    {
        var line = new Line(new Point2D(-1.5, 3.4), new Point2D(2.2, 5.7), White);

        line.Bounds.Should().Be(new BoundingBox(-1.5, 3.4, 2.2, 5.7));
    }

    [Fact]
    public void Bounds_are_normalised_when_the_line_runs_down_and_to_the_left()
    {
        var line = new Line(new Point2D(10, 10), new Point2D(-10, -10), White);

        line.Bounds.Should().Be(new BoundingBox(-10, -10, 10, 10));
    }

    [Fact]
    public void Bounds_of_a_horizontal_line_are_degenerate_in_Y()
    {
        var line = new Line(new Point2D(-5, 2), new Point2D(5, 2), White);

        line.Bounds.Height.Should().Be(0);
        line.Bounds.Width.Should().Be(10);
    }

    [Fact]
    public void A_line_is_not_fillable()
    {
        // A line has no interior, so it must not carry a meaningless Filled flag.
        new Line(Point2D.Origin, new Point2D(1, 1), White).Should().NotBeAssignableTo<IFillablePrimitive>();
    }
}

public class CircleTests
{
    private static readonly ArgbColor Red = new(127, 255, 0, 0);

    [Fact]
    public void Bounds_are_the_centre_expanded_by_the_radius()
    {
        var circle = new Circle(Point2D.Origin, 15, Red, Filled: false);

        circle.Bounds.Should().Be(new BoundingBox(-15, -15, 15, 15));
    }

    [Fact]
    public void Bounds_follow_a_centre_away_from_the_origin()
    {
        var circle = new Circle(new Point2D(-4, 7.5), 2.5, Red, Filled: true);

        circle.Bounds.Should().Be(new BoundingBox(-6.5, 5, -1.5, 10));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reports_its_filled_state(bool filled)
    {
        var circle = new Circle(Point2D.Origin, 1, Red, filled);

        circle.Filled.Should().Be(filled);
        ((IFillablePrimitive)circle).Filled.Should().Be(filled);
    }
}

public class TriangleTests
{
    private static readonly ArgbColor Magenta = new(127, 255, 0, 255);

    private static Triangle Sample(bool filled = true) => new(
        new Point2D(-15, -20),
        new Point2D(15, -20.3),
        new Point2D(0, 21),
        Magenta,
        filled);

    [Fact]
    public void Bounds_span_all_three_vertices()
    {
        Sample().Bounds.Should().Be(new BoundingBox(-15, -20.3, 15, 21));
    }

    [Fact]
    public void Bounds_work_when_every_vertex_is_negative()
    {
        var triangle = new Triangle(
            new Point2D(-10, -10), new Point2D(-2, -30), new Point2D(-40, -5), Magenta, Filled: false);

        triangle.Bounds.Should().Be(new BoundingBox(-40, -30, -2, -5));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reports_its_filled_state(bool filled)
    {
        Sample(filled).Filled.Should().Be(filled);
    }
}
