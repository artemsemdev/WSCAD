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
/// Proves the central extensibility claim: a brand-new primitive can be added without
/// modifying a single production type.
/// </summary>
/// <remarks>
/// Everything the new primitive needs — the domain record and its renderer — is declared in
/// this test project and driven through the <b>unmodified</b> production pipeline. If adding
/// a primitive ever required touching <c>Scene</c>, <c>ViewportTransform</c>, <c>SceneRenderer</c>
/// or the WPF layer, this test could not compile.
/// </remarks>
public class ExtensibilityTests
{
    private static readonly ArgbColor Green = new(255, 0, 255, 0);

    [Fact]
    public void A_new_primitive_type_renders_after_a_single_registration()
    {
        var registry = PrimitiveRendererRegistry.CreateDefault()
            .Register(new RectangleRenderer());          // ← the entire cost of a new primitive
        var scene = new Scene([new Rectangle(new Point2D(-20, -10), 40, 20, Green, Filled: true)]);

        var rendered = new SceneRenderer(registry)
            .Render(scene, new ViewportSize(200, 200), new ViewportFitOptions(Padding: 0));

        var polygon = rendered.Commands.Should().ContainSingle()
            .Which.Should().BeOfType<DrawPolygon>().Subject;
        polygon.Points.Should().HaveCount(4);
        polygon.Appearance.Fill.Should().Be(Green, "the filled rule is inherited, not reimplemented");
    }

    [Fact]
    public void The_new_primitive_takes_part_in_scene_bounds_and_fit_without_any_change_to_them()
    {
        var scene = new Scene([
            new Circle(Point2D.Origin, 5, Green, Filled: false),
            new Rectangle(new Point2D(-100, -50), 200, 100, Green, Filled: false),
        ]);

        scene.Bounds.Should().Be(new BoundingBox(-100, -50, 100, 50));

        var rendered = new SceneRenderer(PrimitiveRendererRegistry.CreateDefault().Register(new RectangleRenderer()))
            .Render(scene, new ViewportSize(100, 100), new ViewportFitOptions(Padding: 0));

        rendered.Scale.Should().Be(0.5, "the 200-unit-wide rectangle is the binding constraint");
    }

    [Fact]
    public void A_registration_can_override_the_rendering_of_a_built_in_primitive()
    {
        // Useful for a host that wants a different look; also proves registration is a
        // replacement rather than an append.
        var registry = PrimitiveRendererRegistry.CreateDefault().Register(new NoOpCircleRenderer());
        var scene = new Scene([new Circle(Point2D.Origin, 5, Green, Filled: true)]);

        var rendered = new SceneRenderer(registry).Render(scene, new ViewportSize(200, 200));

        rendered.Commands.Should().BeEmpty();
    }

    /// <summary>A rectangle primitive — the challenge's own example of a future extension.</summary>
    private sealed record Rectangle(Point2D Origin, double Width, double Height, ArgbColor Color, bool Filled)
        : IFillablePrimitive
    {
        public BoundingBox Bounds =>
            BoundingBox.FromCorners(Origin, new Point2D(Origin.X + Width, Origin.Y + Height));

        public IReadOnlyList<Point2D> Corners =>
        [
            Origin,
            new(Origin.X, Origin.Y + Height),
            new(Origin.X + Width, Origin.Y + Height),
            new(Origin.X + Width, Origin.Y),
        ];
    }

    private sealed class RectangleRenderer : PrimitiveRenderer<Rectangle>
    {
        protected override void Render(Rectangle rectangle, RenderContext context, ICollection<DrawCommand> output)
        {
            var points = new ScreenPoint[rectangle.Corners.Count];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = context.ToScreen(rectangle.Corners[i]);
            }

            output.Add(new DrawPolygon(points, context.AppearanceFor(rectangle)));
        }
    }

    private sealed class NoOpCircleRenderer : PrimitiveRenderer<Circle>
    {
        protected override void Render(Circle circle, RenderContext context, ICollection<DrawCommand> output)
        {
            // Intentionally draws nothing.
        }
    }
}

public class PrimitiveRendererRegistryTests
{
    [Fact]
    public void The_default_registry_covers_every_built_in_primitive()
    {
        var registry = PrimitiveRendererRegistry.CreateDefault();

        registry.TryGetRenderer(typeof(Line), out _).Should().BeTrue();
        registry.TryGetRenderer(typeof(Circle), out _).Should().BeTrue();
        registry.TryGetRenderer(typeof(Triangle), out _).Should().BeTrue();
    }

    [Fact]
    public void Registration_is_chainable()
    {
        var registry = PrimitiveRendererRegistry.CreateDefault();

        registry.Register(new LineRenderer()).Should().BeSameAs(registry);
    }

    [Fact]
    public void Resolving_an_unregistered_primitive_names_the_offending_type()
    {
        var act = () => PrimitiveRendererRegistry.CreateDefault().GetRenderer(new PlaceholderPrimitive());

        act.Should().Throw<NotSupportedException>().WithMessage("*PlaceholderPrimitive*");
    }

    private sealed record PlaceholderPrimitive : IPrimitive
    {
        public ArgbColor Color => new(255, 0, 0, 0);

        public BoundingBox Bounds => new(0, 0, 0, 0);
    }
}
