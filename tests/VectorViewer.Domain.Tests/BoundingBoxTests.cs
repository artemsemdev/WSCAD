using FluentAssertions;
using VectorViewer.Domain.Geometry;
using Xunit;

namespace VectorViewer.Domain.Tests;

public class BoundingBoxTests
{
    [Fact]
    public void FromCorners_normalises_corners_given_in_any_order()
    {
        var fromTopRight = BoundingBox.FromCorners(new Point2D(10, 20), new Point2D(-5, -8));
        var fromBottomLeft = BoundingBox.FromCorners(new Point2D(-5, -8), new Point2D(10, 20));

        fromTopRight.Should().Be(fromBottomLeft);
        fromTopRight.MinX.Should().Be(-5);
        fromTopRight.MinY.Should().Be(-8);
        fromTopRight.MaxX.Should().Be(10);
        fromTopRight.MaxY.Should().Be(20);
    }

    [Fact]
    public void Width_and_height_are_the_extents()
    {
        var box = new BoundingBox(-5, -8, 10, 20);

        box.Width.Should().Be(15);
        box.Height.Should().Be(28);
    }

    [Fact]
    public void Center_is_the_midpoint_even_for_boxes_crossing_the_origin()
    {
        var box = new BoundingBox(-10, -4, 30, 8);

        box.Center.Should().Be(new Point2D(10, 2));
    }

    [Fact]
    public void A_point_produces_a_degenerate_box_with_zero_extent()
    {
        var box = BoundingBox.FromCorners(new Point2D(3, 4), new Point2D(3, 4));

        box.Width.Should().Be(0);
        box.Height.Should().Be(0);
        box.Center.Should().Be(new Point2D(3, 4));
    }

    [Fact]
    public void FromPoints_spans_every_point()
    {
        var box = BoundingBox.FromPoints(
            new Point2D(-15, -20),
            new Point2D(15, -20.3),
            new Point2D(0, 21));

        box.MinX.Should().Be(-15);
        box.MinY.Should().Be(-20.3);
        box.MaxX.Should().Be(15);
        box.MaxY.Should().Be(21);
    }

    [Fact]
    public void FromCenter_expands_by_the_half_extents()
    {
        var box = BoundingBox.FromCenter(new Point2D(2, -3), halfWidth: 15, halfHeight: 15);

        box.Should().Be(new BoundingBox(-13, -18, 17, 12));
    }

    [Fact]
    public void Union_contains_both_boxes()
    {
        var left = new BoundingBox(-10, 0, -2, 5);
        var right = new BoundingBox(4, -6, 9, 1);

        left.Union(right).Should().Be(new BoundingBox(-10, -6, 9, 5));
    }

    [Fact]
    public void Union_is_commutative()
    {
        var a = new BoundingBox(-10, 0, -2, 5);
        var b = new BoundingBox(4, -6, 9, 1);

        a.Union(b).Should().Be(b.Union(a));
    }

    [Fact]
    public void Union_with_a_contained_box_changes_nothing()
    {
        var outer = new BoundingBox(-10, -10, 10, 10);
        var inner = new BoundingBox(-1, -1, 1, 1);

        outer.Union(inner).Should().Be(outer);
    }

    [Theory]
    [InlineData(0, 0, true)]      // interior
    [InlineData(-10, -10, true)]  // corner counts as inside
    [InlineData(10, 10, true)]
    [InlineData(-10.1, 0, false)]
    [InlineData(0, 11, false)]
    public void Contains_treats_the_boundary_as_inside(double x, double y, bool expected)
    {
        var box = new BoundingBox(-10, -10, 10, 10);

        box.Contains(new Point2D(x, y)).Should().Be(expected);
    }
}
