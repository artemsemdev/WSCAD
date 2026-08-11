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

        // Copied so a later edit to the caller's collection cannot reach the scene.
        _primitives = [.. primitives];

        // Wrapped so the copy cannot be edited either. Publishing the array directly would
        // let a caller cast it back — `(IPrimitive[])scene.Primitives` — and replace an
        // element in place, after which the cached bounds below would describe a drawing that
        // no longer exists. The wrapper is built once here, so reading Primitives stays
        // allocation-free on the redraw path.
        Primitives = Array.AsReadOnly(_primitives);

        Bounds = ComputeBounds(_primitives);
    }

    public static Scene Empty { get; } = new([]);

    public IReadOnlyList<IPrimitive> Primitives { get; }

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
