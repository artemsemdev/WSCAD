using FluentAssertions;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Application.Tests;

public class RenderContextTests
{
    private static readonly ArgbColor Red = new(127, 255, 0, 0);

    private static RenderContext ContextAtScale(double scale, RenderOptions? options = null) =>
        new(new ViewportTransform(scale, Point2D.Origin, new ScreenPoint(100, 100)),
            options ?? RenderOptions.Default);

    [Fact]
    public void The_border_is_one_world_unit_wide_so_it_scales_with_the_drawing()
    {
        ContextAtScale(2).StrokeFor(Red).Thickness.Should().Be(2);
        ContextAtScale(10).StrokeFor(Red).Thickness.Should().Be(10);
    }

    [Fact]
    public void The_border_never_falls_below_one_pixel_on_a_heavily_scaled_down_drawing()
    {
        // At 0.02 scale a 1-unit border would be 0.02px — invisible. It is clamped instead.
        ContextAtScale(0.02).StrokeFor(Red).Thickness.Should().Be(1);
    }

    [Fact]
    public void The_border_keeps_the_primitive_colour()
    {
        ContextAtScale(1).StrokeFor(Red).Color.Should().Be(Red);
    }

    [Fact]
    public void Border_width_is_configurable()
    {
        var thick = ContextAtScale(1, new RenderOptions(BorderWidthInWorldUnits: 3));

        thick.StrokeFor(Red).Thickness.Should().Be(3);
    }

    [Fact]
    public void A_filled_primitive_gets_both_a_border_and_a_fill_in_its_own_colour()
    {
        var appearance = ContextAtScale(1).AppearanceFor(new Circle(Point2D.Origin, 5, Red, Filled: true));

        appearance.Stroke!.Value.Color.Should().Be(Red);
        appearance.Fill.Should().Be(Red);
    }

    [Fact]
    public void An_unfilled_primitive_gets_a_border_only()
    {
        var appearance = ContextAtScale(1).AppearanceFor(new Circle(Point2D.Origin, 5, Red, Filled: false));

        appearance.Stroke.Should().NotBeNull();
        appearance.Fill.Should().BeNull("filled: false means border only");
    }

    [Fact]
    public void A_primitive_that_cannot_be_filled_gets_a_border_only()
    {
        var appearance = ContextAtScale(1).AppearanceFor(new Line(Point2D.Origin, new Point2D(1, 1), Red));

        appearance.Stroke.Should().NotBeNull();
        appearance.Fill.Should().BeNull();
    }

    [Fact]
    public void Points_are_converted_through_the_transform()
    {
        ContextAtScale(2).ToScreen(new Point2D(10, 5)).ShouldBe(120, 90);
    }

    [Fact]
    public void Lengths_are_converted_through_the_transform()
    {
        ContextAtScale(2).ToScreenLength(15).Should().Be(30);
    }
}
