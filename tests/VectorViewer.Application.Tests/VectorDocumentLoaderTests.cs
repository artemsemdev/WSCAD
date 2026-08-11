using FluentAssertions;
using VectorViewer.Application.Documents;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// Format selection is the application's only knowledge of input formats. The stub reader
/// below is hand-written rather than mocked: the port is small enough that a fake is clearer
/// than a mocking framework, and it doubles as a demonstration of adding a format.
/// </summary>
public class VectorDocumentLoaderTests
{
    [Fact]
    public void The_reader_matching_the_file_extension_is_used()
    {
        var json = new StubReader(".json");
        var xml = new StubReader(".xml");
        var loader = new VectorDocumentLoader([json, xml]);

        loader.Load(Stream.Null, "drawing.xml");

        xml.WasUsed.Should().BeTrue();
        json.WasUsed.Should().BeFalse();
    }

    [Fact]
    public void Extension_matching_ignores_case()
    {
        var reader = new StubReader(".json");

        new VectorDocumentLoader([reader]).Load(Stream.Null, "DRAWING.JSON");

        reader.WasUsed.Should().BeTrue();
    }

    [Fact]
    public void A_reader_may_declare_several_extensions()
    {
        var reader = new StubReader(".json", ".vec");

        new VectorDocumentLoader([reader]).Load(Stream.Null, "drawing.vec");

        reader.WasUsed.Should().BeTrue();
    }

    [Fact]
    public void An_unsupported_extension_fails_with_a_message_naming_it()
    {
        var loader = new VectorDocumentLoader([new StubReader(".json")]);

        var act = () => loader.Load(Stream.Null, "drawing.dxf");

        act.Should().Throw<NotSupportedException>().WithMessage("*.dxf*");
    }

    [Fact]
    public void Supported_extensions_are_aggregated_so_the_UI_need_not_hardcode_them()
    {
        var loader = new VectorDocumentLoader([new StubReader(".json"), new StubReader(".xml", ".xaml")]);

        loader.SupportedExtensions.Should().BeEquivalentTo([".json", ".xml", ".xaml"]);
    }

    [Fact]
    public void Loading_a_file_from_disk_delegates_to_the_matching_reader()
    {
        var reader = new StubReader(".json");
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "[]");

        try
        {
            var scene = new VectorDocumentLoader([reader]).Load(path);

            reader.WasUsed.Should().BeTrue();
            scene.Primitives.Should().HaveCount(1, "the stub reader returns one primitive");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubReader(params string[] extensions) : IVectorDocumentReader
    {
        public bool WasUsed { get; private set; }

        public IReadOnlyCollection<string> SupportedExtensions => extensions;

        public string FormatName => "Stub";

        public Scene Read(Stream stream)
        {
            WasUsed = true;
            return new Scene([new Circle(Point2D.Origin, 1, new ArgbColor(255, 0, 0, 0), Filled: false)]);
        }
    }
}
