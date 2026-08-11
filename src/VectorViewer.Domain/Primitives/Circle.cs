using VectorViewer.Domain.Geometry;

namespace VectorViewer.Domain.Primitives;

/// <summary>A circle defined by its centre and radius in world units.</summary>
public sealed record Circle(Point2D Center, double Radius, ArgbColor Color, bool Filled)
    : IFillablePrimitive
{
    public BoundingBox Bounds => BoundingBox.FromCenter(Center, Radius, Radius);
}
