using FluentAssertions;
using VectorViewer.Application.Documents;
using VectorViewer.Infrastructure.Json;
using Xunit;

namespace VectorViewer.Infrastructure.Tests;

/// <summary>
/// The reader's supported extensions are backed by a <c>static readonly</c> array shared by
/// every instance in the process. Publishing it directly makes the damage from a cast wider
/// than usual: an edit would not affect one reader, it would affect all of them, including
/// readers created later.
/// </summary>
public class PublishedCollectionTests
{
    [Fact]
    public void The_extension_list_is_not_the_shared_static_array()
    {
        new JsonVectorDocumentReader().SupportedExtensions.Should().NotBeAssignableTo<string[]>();
    }

    [Fact]
    public void The_extension_list_cannot_be_emptied_by_a_consumer()
    {
        var reader = new JsonVectorDocumentReader();

        var act = () => ((ICollection<string>)reader.SupportedExtensions).Clear();

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void One_reader_cannot_corrupt_the_extensions_seen_by_another()
    {
        // The consequence of the array being static: format selection is process-wide state.
        var first = new JsonVectorDocumentReader();

        try
        {
            ((IList<string>)first.SupportedExtensions)[0] = ".corrupted";
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidCastException)
        {
            // Expected — what matters is the state afterwards.
        }

        new JsonVectorDocumentReader().SupportedExtensions.Should().Contain(".json");
        new VectorDocumentLoader([new JsonVectorDocumentReader()])
            .SupportedExtensions.Should().Contain(".json");
    }
}
