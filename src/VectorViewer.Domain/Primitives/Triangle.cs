using VectorViewer.Domain.Geometry;

namespace VectorViewer.Domain.Primitives;

/// <summary>A triangle defined by its three vertices.</summary>
public sealed record Triangle(Point2D A, Point2D B, Point2D C, ArgbColor Color, bool Filled)
    : IFillablePrimitive
{
    public BoundingBox Bounds => BoundingBox.FromPoints(A, B, C);
}
