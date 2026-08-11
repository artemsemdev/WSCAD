namespace VectorViewer.Domain.Geometry;

/// <summary>
/// A point in Cartesian world space, in virtual units, with the Y axis pointing up.
/// </summary>
public readonly record struct Point2D(double X, double Y)
{
    public static Point2D Origin => new(0, 0);

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"({X}; {Y})");
}
