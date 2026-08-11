using VectorViewer.Domain.Geometry;

namespace VectorViewer.Domain.Primitives;

/// <summary>A straight segment between two points. Has no interior, so it is not fillable.</summary>
public sealed record Line(Point2D Start, Point2D End, ArgbColor Color) : IPrimitive
{
    public BoundingBox Bounds => BoundingBox.FromCorners(Start, End);
}
