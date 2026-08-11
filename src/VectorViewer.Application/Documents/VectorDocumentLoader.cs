using VectorViewer.Domain;

namespace VectorViewer.Application.Documents;

/// <summary>
/// Loads a scene from a file by selecting the reader that supports the file's extension.
/// </summary>
/// <remarks>
/// Format selection lives here so the UI never has to know which formats exist; adding XML
/// support is a constructor argument, not a change to this class.
/// </remarks>
public sealed class VectorDocumentLoader
{
    private readonly Dictionary<string, IVectorDocumentReader> _byExtension =
        new(StringComparer.OrdinalIgnoreCase);

    public VectorDocumentLoader(IEnumerable<IVectorDocumentReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);

        Readers = [.. readers];
        foreach (var reader in Readers)
        {
            foreach (var extension in reader.SupportedExtensions)
            {
                // First registration wins, so the caller's ordering decides which reader
                // owns an extension claimed by more than one.
                _byExtension.TryAdd(extension, reader);
            }
        }
    }

    public IReadOnlyList<IVectorDocumentReader> Readers { get; }

    /// <summary>Every extension supported by any registered reader, e.g. for a file dialog filter.</summary>
    public IReadOnlyCollection<string> SupportedExtensions => _byExtension.Keys;

    /// <exception cref="NotSupportedException">No registered reader handles the file's extension.</exception>
    public Scene Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return Load(stream, path);
    }

    /// <summary>Reads from an open stream, choosing the reader by the extension of <paramref name="fileName"/>.</summary>
    public Scene Load(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var extension = Path.GetExtension(fileName);
        if (!_byExtension.TryGetValue(extension, out var reader))
        {
            throw new NotSupportedException(
                $"No reader is registered for '{extension}' files. " +
                $"Supported: {string.Join(", ", SupportedExtensions)}.");
        }

        return reader.Read(stream);
    }
}
