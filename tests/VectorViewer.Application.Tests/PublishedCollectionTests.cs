using FluentAssertions;
using VectorViewer.Application.Documents;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// Collections handed out across a public API must not be castable back to the mutable type
/// behind them. The rule applied here is deliberate: wrap wherever a mutation would actually
/// succeed, and do not copy on the redraw path — wrapping already closes the hole and costs
/// nothing per frame.
/// </summary>
public class PublishedCollectionTests
{
    private static readonly ArgbColor AnyColor = new(255, 1, 2, 3);

    public class RenderedSceneCommands
    {
        private static RenderedScene Render() =>
            new SceneRenderer(PrimitiveRendererRegistry.CreateDefault())
                .Render(new Scene([new Circle(Point2D.Origin, 5, AnyColor, Filled: true)]),
                        new ViewportSize(800, 600));

        [Fact]
        public void The_command_list_is_not_the_builder_list()
        {
            Render().Commands.Should().NotBeAssignableTo<List<DrawCommand>>();
        }

        [Fact]
        public void Commands_cannot_be_replaced_by_a_consumer()
        {
            var rendered = Render();
            var replacement = new DrawLine(new ScreenPoint(0, 0), new ScreenPoint(1, 1),
                                           new Appearance(null, null));

            var act = () => ((IList<DrawCommand>)rendered.Commands)[0] = replacement;

            act.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void An_empty_rendered_scene_is_equally_protected()
        {
            var act = () => ((IList<DrawCommand>)RenderedScene.Empty.Commands)
                .Add(new DrawLine(new ScreenPoint(0, 0), new ScreenPoint(1, 1),
                                  new Appearance(null, null)));

            act.Should().Throw<NotSupportedException>();
        }
    }

    public class PolygonPoints
    {
        private static DrawPolygon RenderedTriangle()
        {
            var output = new List<DrawCommand>();
            var context = new RenderContext(
                new ViewportTransform(1, Point2D.Origin, new ScreenPoint(0, 0)),
                RenderOptions.Default);
            IPrimitiveRenderer renderer = new Rendering.Renderers.TriangleRenderer();
            renderer.Render(
                new Triangle(Point2D.Origin, new Point2D(1, 0), new Point2D(0, 1), AnyColor, true),
                context, output);
            return (DrawPolygon)output.Single();
        }

        [Fact]
        public void A_polygon_does_not_publish_its_vertex_array()
        {
            RenderedTriangle().Points.Should().NotBeAssignableTo<ScreenPoint[]>();
        }

        [Fact]
        public void Polygon_vertices_cannot_be_moved_by_a_consumer()
        {
            var polygon = RenderedTriangle();

            var act = () => ((IList<ScreenPoint>)polygon.Points)[0] = new ScreenPoint(999, 999);

            act.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void The_protection_holds_for_a_polygon_built_by_any_renderer()
        {
            // A future renderer will pass a plain array too; the command type itself has to
            // be the thing that refuses, not each renderer remembering to wrap.
            var polygon = new DrawPolygon(
                [new ScreenPoint(0, 0), new ScreenPoint(1, 0), new ScreenPoint(0, 1)],
                new Appearance(null, null));

            var act = () => ((IList<ScreenPoint>)polygon.Points)[0] = new ScreenPoint(9, 9);

            act.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void Wrapping_does_not_change_the_vertices()
        {
            var polygon = RenderedTriangle();

            polygon.Points.Should().HaveCount(3);
            polygon.Points[0].Should().Be(new ScreenPoint(0, 0));
        }
    }

    public class LoaderReaders
    {
        private static VectorDocumentLoader Loader() => new([new StubReader()]);

        [Fact]
        public void The_reader_list_is_not_the_backing_array()
        {
            Loader().Readers.Should().NotBeAssignableTo<IVectorDocumentReader[]>();
        }

        [Fact]
        public void A_configured_reader_cannot_be_swapped_out_later()
        {
            // The loader is built once at start-up and shared; silently replacing a reader
            // would change how every subsequent file is parsed.
            var loader = Loader();

            var act = () => ((IList<IVectorDocumentReader>)loader.Readers)[0] = new StubReader();

            act.Should().Throw<NotSupportedException>();
        }

        private sealed class StubReader : IVectorDocumentReader
        {
            public IReadOnlyCollection<string> SupportedExtensions => [".stub"];

            public string FormatName => "Stub";

            public Scene Read(Stream stream) => Scene.Empty;
        }
    }
}
