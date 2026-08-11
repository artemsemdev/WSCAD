using VectorViewer.Application.Viewport;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Application.Rendering;

/// <summary>
/// Everything a primitive renderer needs: the active transform and the appearance policy.
/// </summary>
/// <remarks>
/// Centralising <see cref="AppearanceFor"/> here means the "filled ⇒ border + fill, otherwise
/// border only" rule is expressed once rather than repeated — and correctly inherited — by
/// every renderer, including future ones.
/// </remarks>
public sealed class RenderContext(ViewportTransform transform, RenderOptions options)
{
    /// <summary>
    /// The border is the same for every primitive in a redraw, so it is computed once here
    /// rather than per primitive inside the render loop.
    /// </summary>
    private readonly double _strokeThickness = Math.Max(
        transform.ToScreenLength(options.BorderWidthInWorldUnits),
        options.MinimumBorderWidthInPixels);

    public ViewportTransform Transform { get; } = transform;

    public RenderOptions Options { get; } = options;

    public ScreenPoint ToScreen(Point2D world) => Transform.ToScreen(world);

    public double ToScreenLength(double worldLength) => Transform.ToScreenLength(worldLength);

    /// <summary>The border for a primitive of the given colour, with its thickness in pixels.</summary>
    public Stroke StrokeFor(ArgbColor color) => new(color, _strokeThickness);

    /// <summary>
    /// How the given primitive should be painted: always a border in its own colour, plus a
    /// fill in the same colour when the primitive is fillable and its <c>Filled</c> flag is set.
    /// </summary>
    public Appearance AppearanceFor(IPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);

        var fill = primitive is IFillablePrimitive { Filled: true } ? primitive.Color : (ArgbColor?)null;
        return new Appearance(StrokeFor(primitive.Color), fill);
    }
}
