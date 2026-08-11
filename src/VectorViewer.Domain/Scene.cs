using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Domain;

/// <summary>
/// An immutable, ordered collection of primitives — the result of reading one input document.
/// </summary>
/// <remarks>
/// Order is significant: primitives are drawn in document order, so later primitives paint
/// over earlier ones. The scene's bounds are computed once at construction because they are
/// read on every redraw (window resize) but never change.
/// </remarks>
public sealed class Scene
{
    private readonly IPrimitive[] _primitives;

    public Scene(IEnumerable<IPrimitive> primitives)
    {
        ArgumentNullException.ThrowIfNull(primitives);

        // Copied so the scene cannot be mutated behind the caller's back — it is shared with
        // the UI thread and its cached bounds must stay truthful.
        _primitives = [.. primitives];
        Bounds = ComputeBounds(_primitives);
    }

    public static Scene Empty { get; } = new([]);

    public IReadOnlyList<IPrimitive> Primitives => _primitives;

    /// <summary>
    /// The smallest box containing every primitive, or <c>null</c> when the scene is empty.
    /// </summary>
    /// <remarks>
    /// Deliberately nullable rather than an "empty box" sentinel: an empty box has no
    /// meaningful centre or extent, and silently participating in fit calculations would
    /// produce a plausible but wrong transform.
    /// </remarks>
    public BoundingBox? Bounds { get; }

    public bool IsEmpty => _primitives.Length == 0;

    private static BoundingBox? ComputeBounds(IPrimitive[] primitives)
    {
        if (primitives.Length == 0)
        {
            return null;
        }

        var bounds = primitives[0].Bounds;
        for (var i = 1; i < primitives.Length; i++)
        {
            bounds = bounds.Union(primitives[i].Bounds);
        }

        return bounds;
    }
}
