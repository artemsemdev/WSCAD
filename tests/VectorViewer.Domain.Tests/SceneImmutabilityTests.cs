using FluentAssertions;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Domain.Tests;

/// <summary>
/// A <see cref="Scene"/> documents itself as immutable and caches <see cref="Scene.Bounds"/>
/// on that basis. Copying the input is not enough: publishing the copy as a bare array lets a
/// caller cast it back and edit it in place, after which the cached bounds silently disagree
/// with the contents. These tests pin the invariant the class actually promises.
/// </summary>
public class SceneImmutabilityTests
{
    private static readonly ArgbColor AnyColor = new(255, 1, 2, 3);

    private static Scene SceneWithOneSmallCircle() =>
        new([new Circle(Point2D.Origin, 1, AnyColor, Filled: false)]);

    private static Circle HugeCircle => new(Point2D.Origin, 1000, AnyColor, Filled: false);

    [Fact]
    public void The_published_collection_is_not_the_backing_array()
    {
        // The exact escape route: `(IPrimitive[])scene.Primitives` must not succeed.
        SceneWithOneSmallCircle().Primitives.Should().NotBeAssignableTo<IPrimitive[]>();
    }

    [Fact]
    public void The_published_collection_is_not_a_mutable_list()
    {
        SceneWithOneSmallCircle().Primitives.Should().NotBeAssignableTo<List<IPrimitive>>();
    }

    [Fact]
    public void Replacing_an_element_through_the_published_collection_is_refused()
    {
        var scene = SceneWithOneSmallCircle();

        var act = () => ((IList<IPrimitive>)scene.Primitives)[0] = HugeCircle;

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Adding_through_the_published_collection_is_refused()
    {
        var scene = SceneWithOneSmallCircle();

        var act = () => ((ICollection<IPrimitive>)scene.Primitives).Add(HugeCircle);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Cached_bounds_can_never_disagree_with_the_published_contents()
    {
        // The consequence that makes this worth guarding: bounds are computed once, so a
        // successful mutation would leave the viewer fitting a drawing that no longer exists.
        var scene = SceneWithOneSmallCircle();
        var boundsAtConstruction = scene.Bounds;

        try
        {
            ((IList<IPrimitive>)scene.Primitives)[0] = HugeCircle;
        }
        catch (NotSupportedException)
        {
            // Expected — the point is what must hold afterwards.
        }

        scene.Bounds.Should().Be(boundsAtConstruction);
        scene.Bounds.Should().Be(scene.Primitives[0].Bounds, "the cache still matches reality");
    }

    [Fact]
    public void Mutating_the_source_collection_afterwards_does_not_reach_the_scene()
    {
        // Already covered by the input being copied, kept here so both halves of the
        // invariant — input and output — are pinned together.
        var source = new List<IPrimitive> { new Circle(Point2D.Origin, 1, AnyColor, Filled: false) };
        var scene = new Scene(source);

        source[0] = HugeCircle;
        source.Add(HugeCircle);

        scene.Primitives.Should().HaveCount(1);
        ((Circle)scene.Primitives[0]).Radius.Should().Be(1);
    }
}
