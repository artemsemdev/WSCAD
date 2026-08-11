namespace VectorViewer.Application.Viewport;

/// <summary>
/// A point in device space: origin top-left, X right, Y <b>down</b>, measured in pixels.
/// Distinct from <see cref="Domain.Geometry.Point2D"/> so the two spaces cannot be mixed up
/// by accident — the compiler enforces that a transform is applied.
/// </summary>
public readonly record struct ScreenPoint(double X, double Y)
{
    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"({X}, {Y})");
}

/// <summary>The drawable area available to the viewer, in pixels.</summary>
public readonly record struct ViewportSize(double Width, double Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
