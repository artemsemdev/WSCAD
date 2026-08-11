using VectorViewer.Domain.Geometry;

namespace VectorViewer.Domain.Primitives;

/// <summary>
/// A drawable vector primitive.
/// </summary>
/// <remarks>
/// A primitive knows its colour and its extent — deliberately nothing about how it is drawn.
/// Rendering is resolved outside the domain by <c>PrimitiveRendererRegistry</c>, so adding a
/// primitive type does not touch any existing type. See docs/adr/0001-architecture.md.
/// </remarks>
public interface IPrimitive
{
    ArgbColor Color { get; }

    /// <summary>The smallest axis-aligned box in world space containing this primitive's geometry.</summary>
    BoundingBox Bounds { get; }
}

/// <summary>
/// A primitive that encloses an area and can therefore be filled.
/// </summary>
/// <remarks>
/// Modelled separately from <see cref="IPrimitive"/> because a line has no interior:
/// putting <c>Filled</c> on the base type would force a meaningless flag onto such primitives.
/// </remarks>
public interface IFillablePrimitive : IPrimitive
{
    /// <summary>When true the shape is drawn with border and fill, otherwise border only.</summary>
    bool Filled { get; }
}
