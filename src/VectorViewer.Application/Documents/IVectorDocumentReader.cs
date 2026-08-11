using VectorViewer.Domain;

namespace VectorViewer.Application.Documents;

/// <summary>
/// Reads a scene from a document stream in one particular format.
/// </summary>
/// <remarks>
/// The port that keeps the input format replaceable: it is declared by the application layer
/// and implemented by an adapter (JSON today, XML next), so nothing in the core names a format.
/// </remarks>
public interface IVectorDocumentReader
{
    /// <summary>File extensions this reader handles, including the dot, e.g. <c>".json"</c>.</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    /// <summary>A short name for the format, for file dialogs and status messages.</summary>
    string FormatName { get; }

    Scene Read(Stream stream);
}
