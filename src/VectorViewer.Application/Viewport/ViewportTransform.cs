using VectorViewer.Domain.Geometry;

namespace VectorViewer.Application.Viewport;

/// <summary>
/// Converts between Cartesian world space (Y up) and device space (Y down), applying a
/// uniform scale and centring the drawing in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// This is the single place in the solution that knows the Y axis is inverted. It is a pure
/// value with no UI dependency, so all the fit/scale/centre behaviour is covered by ordinary
/// unit tests.
/// </para>
/// <para>
/// The mapping is:
/// <code>
/// screenX = ScreenOrigin.X + (worldX - WorldCenter.X) * Scale
/// screenY = ScreenOrigin.Y - (worldY - WorldCenter.Y) * Scale
/// </code>
/// where <c>ScreenOrigin</c> is the pixel position onto which <c>WorldCenter</c> is mapped.
/// </para>
/// </remarks>
public sealed record ViewportTransform(double Scale, Point2D WorldCenter, ScreenPoint ScreenOrigin)
{
    /// <summary>An unscaled transform mapping the world origin to the top-left pixel.</summary>
    public static ViewportTransform Identity { get; } =
        new(1.0, Point2D.Origin, new ScreenPoint(0, 0));

    /// <summary>Scale expressed as a percentage, for display (100 % ⇒ 1 unit = 1 pixel).</summary>
    public double ScalePercentage => Scale * 100.0;

    /// <summary>
    /// Builds the transform that fits <paramref name="bounds"/> into <paramref name="viewport"/>:
    /// one uniform scale for both axes (aspect ratio preserved), clamped so the drawing is never
    /// magnified beyond 100 %, with the scene centred.
    /// </summary>
    public static ViewportTransform Fit(
        BoundingBox bounds,
        ViewportSize viewport,
        ViewportFitOptions? options = null)
    {
        options ??= ViewportFitOptions.Default;

        // The scene centre always lands on the viewport centre — that is what centres the drawing.
        var screenOrigin = new ScreenPoint(viewport.Width / 2, viewport.Height / 2);

        // WPF raises layout passes with a zero size before a window is shown.
        if (viewport.IsEmpty)
        {
            return new ViewportTransform(1.0, bounds.Center, screenOrigin);
        }

        // Padding may not eat more than half of a small viewport, otherwise a narrow window
        // would collapse the drawing to nothing.
        var padding = Math.Clamp(options.Padding, 0, Math.Min(viewport.Width, viewport.Height) / 4);
        var available = new ViewportSize(viewport.Width - (2 * padding), viewport.Height - (2 * padding));

        // One factor for both axes preserves the aspect ratio; the smaller one is binding.
        var scale = Math.Min(
            ScaleFor(available.Width, bounds.Width),
            ScaleFor(available.Height, bounds.Height));
        scale = Math.Min(scale, options.MaximumScale);

        // A scene with no extent in either axis imposes no constraint: show it at 100 %.
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1.0;
        }

        return new ViewportTransform(scale, bounds.Center, screenOrigin);
    }

    public ScreenPoint ToScreen(Point2D world) => new(
        ScreenOrigin.X + ((world.X - WorldCenter.X) * Scale),
        ScreenOrigin.Y - ((world.Y - WorldCenter.Y) * Scale));   // ← the Y axis inversion

    /// <summary>The inverse mapping — the basis for future hit-testing of primitives.</summary>
    public Point2D ToWorld(ScreenPoint screen) => new(
        WorldCenter.X + ((screen.X - ScreenOrigin.X) / Scale),
        WorldCenter.Y - ((screen.Y - ScreenOrigin.Y) / Scale));

    /// <summary>Converts a length in world units to pixels (e.g. a radius or a border width).</summary>
    public double ToScreenLength(double worldLength) => worldLength * Scale;

    /// <summary>Converts a length in pixels to world units (e.g. a hit-test tolerance).</summary>
    public double ToWorldLength(double screenLength) => screenLength / Scale;

    /// <summary>
    /// How far one axis may be scaled. An axis with no extent cannot constrain the fit, so it
    /// yields infinity and loses the <c>Math.Min</c> — this is what keeps a single point or a
    /// perfectly horizontal line from dividing by zero.
    /// </summary>
    private static double ScaleFor(double available, double extent) =>
        extent > 0 ? available / extent : double.PositiveInfinity;
}
