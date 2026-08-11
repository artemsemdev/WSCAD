namespace VectorViewer.Domain.Geometry;

/// <summary>
/// An axis-aligned rectangle in world space, used to determine how a scene fits a viewport.
/// A box always satisfies <c>MinX &lt;= MaxX</c> and <c>MinY &lt;= MaxY</c>; a degenerate
/// (zero width and/or height) box is legal and represents a point or an axis-parallel segment.
/// </summary>
public readonly record struct BoundingBox
{
    /// <remarks>
    /// The caller is trusted to pass ordered bounds; use <see cref="FromCorners"/> when the
    /// ordering is not already known. Every factory on this type maintains the invariant.
    /// </remarks>
    public BoundingBox(double minX, double minY, double maxX, double maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public double MinX { get; }

    public double MinY { get; }

    public double MaxX { get; }

    public double MaxY { get; }

    public double Width => MaxX - MinX;

    public double Height => MaxY - MinY;

    public Point2D Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2);

    /// <summary>Builds the smallest box containing two opposite corners, in any order.</summary>
    public static BoundingBox FromCorners(Point2D first, Point2D second) => new(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y),
        Math.Max(first.X, second.X),
        Math.Max(first.Y, second.Y));

    /// <summary>Builds the smallest box containing all the given points.</summary>
    /// <exception cref="ArgumentException"><paramref name="points"/> is empty.</exception>
    public static BoundingBox FromPoints(params Point2D[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length == 0)
        {
            throw new ArgumentException("A bounding box needs at least one point.", nameof(points));
        }

        double minX = points[0].X, minY = points[0].Y;
        double maxX = minX, maxY = minY;

        for (var i = 1; i < points.Length; i++)
        {
            var point = points[i];
            if (point.X < minX) minX = point.X;
            if (point.X > maxX) maxX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.Y > maxY) maxY = point.Y;
        }

        return new BoundingBox(minX, minY, maxX, maxY);
    }

    /// <summary>Builds a box from a centre and half-extents, e.g. a circle's centre and radius.</summary>
    public static BoundingBox FromCenter(Point2D center, double halfWidth, double halfHeight) => new(
        center.X - halfWidth,
        center.Y - halfHeight,
        center.X + halfWidth,
        center.Y + halfHeight);

    /// <summary>The smallest box containing both this box and <paramref name="other"/>.</summary>
    public BoundingBox Union(BoundingBox other) => new(
        Math.Min(MinX, other.MinX),
        Math.Min(MinY, other.MinY),
        Math.Max(MaxX, other.MaxX),
        Math.Max(MaxY, other.MaxY));

    /// <summary>Whether the point lies inside the box; the boundary counts as inside.</summary>
    public bool Contains(Point2D point) =>
        point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"[({MinX}; {MinY}) .. ({MaxX}; {MaxY})]");
}
