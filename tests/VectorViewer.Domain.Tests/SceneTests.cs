using FluentAssertions;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Domain.Tests;

public class SceneTests
{
    private static readonly ArgbColor AnyColor = new(255, 1, 2, 3);

    [Fact]
    public void Bounds_of_a_single_primitive_are_that_primitive_bounds()
    {
        var circle = new Circle(Point2D.Origin, 15, AnyColor, Filled: false);

        new Scene([circle]).Bounds.Should().Be(circle.Bounds);
    }

    [Fact]
    public void Bounds_span_every_primitive_of_mixed_types()
    {
        var scene = new Scene([
            new Line(new Point2D(-1.5, 3.4), new Point2D(2.2, 5.7), AnyColor),
            new Circle(Point2D.Origin, 15, AnyColor, Filled: false),
            new Triangle(new Point2D(-15, -20), new Point2D(15, -20.3), new Point2D(0, 21), AnyColor, Filled: true),
        ]);

        // Widest in X is the circle (±15) tied with the triangle; lowest Y is the triangle
        // (-20.3), highest is the triangle apex (21).
        scene.Bounds.Should().Be(new BoundingBox(-15, -20.3, 15, 21));
    }

    [Fact]
    public void Bounds_of_an_empty_scene_are_null_rather_than_a_misleading_empty_box()
    {
        new Scene([]).Bounds.Should().BeNull();
        Scene.Empty.Bounds.Should().BeNull();
    }

    [Fact]
    public void An_empty_scene_reports_itself_as_empty()
    {
        Scene.Empty.IsEmpty.Should().BeTrue();
        new Scene([new Circle(Point2D.Origin, 1, AnyColor, Filled: false)]).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Primitive_order_is_preserved_because_it_is_the_draw_order()
    {
        var first = new Circle(Point2D.Origin, 1, AnyColor, Filled: true);
        var second = new Line(Point2D.Origin, new Point2D(1, 1), AnyColor);
        var third = new Triangle(Point2D.Origin, new Point2D(1, 0), new Point2D(0, 1), AnyColor, Filled: false);

        new Scene([first, second, third]).Primitives.Should().ContainInOrder(first, second, third);
    }

    [Fact]
    public void The_scene_is_decoupled_from_the_collection_it_was_built_from()
    {
        var source = new List<IPrimitive> { new Circle(Point2D.Origin, 1, AnyColor, Filled: true) };
        var scene = new Scene(source);

        source.Clear();

        scene.Primitives.Should().HaveCount(1);
    }
}
