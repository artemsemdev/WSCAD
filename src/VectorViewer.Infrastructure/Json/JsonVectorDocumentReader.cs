using System.Text.Json;
using VectorViewer.Application.Documents;
using VectorViewer.Domain;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Infrastructure.Json;

/// <summary>
/// Reads a scene from a JSON array of primitive objects, dispatching each element to the
/// mapper registered for its <c>"type"</c> discriminator.
/// </summary>
public sealed class JsonVectorDocumentReader : IVectorDocumentReader
{
    // Wrapped, not a bare array: this is static, so every reader in the process shares it.
    // Publishing the array itself would let one consumer change which files every other
    // reader claims to handle.
    private static readonly IReadOnlyCollection<string> Extensions = Array.AsReadOnly([".json"]);

    /// <summary>Tolerant of hand-edited files; neither affects how valid documents are read.</summary>
    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly Dictionary<string, IPrimitiveJsonMapper> _mappers;

    /// <summary>Creates a reader supporting the built-in primitives.</summary>
    public JsonVectorDocumentReader()
        : this([new LineJsonMapper(), new CircleJsonMapper(), new TriangleJsonMapper()])
    {
    }

    /// <summary>Creates a reader with an explicit mapper set, e.g. to add a new primitive type.</summary>
    public JsonVectorDocumentReader(IEnumerable<IPrimitiveJsonMapper> mappers)
    {
        ArgumentNullException.ThrowIfNull(mappers);

        _mappers = mappers.ToDictionary(mapper => mapper.TypeDiscriminator, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> SupportedExtensions => Extensions;

    public string FormatName => "JSON vector document";

    /// <exception cref="NotSupportedException">An element declares a <c>"type"</c> with no registered mapper.</exception>
    public Scene Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var document = JsonDocument.Parse(stream, ParseOptions);
        var root = document.RootElement;

        var primitives = new List<IPrimitive>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            primitives.Add(MapperFor(element).Map(element));
        }

        return new Scene(primitives);
    }

    private IPrimitiveJsonMapper MapperFor(JsonElement element)
    {
        var discriminator = element.GetProperty("type").GetString();
        if (discriminator is not null && _mappers.TryGetValue(discriminator, out var mapper))
        {
            return mapper;
        }

        // Input is assumed valid, but skipping an unrecognised shape would render a silently
        // wrong drawing — a much harder failure to diagnose than an explicit message.
        throw new NotSupportedException(
            $"Unknown primitive type '{discriminator}'. " +
            $"Known types: {string.Join(", ", _mappers.Keys)}.");
    }
}
