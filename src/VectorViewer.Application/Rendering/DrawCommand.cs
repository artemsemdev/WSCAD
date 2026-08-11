using VectorViewer.Application.Viewport;
using VectorViewer.Domain;

namespace VectorViewer.Application.Rendering;

/// <summary>A border: colour plus thickness in pixels.</summary>
public readonly record struct Stroke(ArgbColor Color, double Thickness);

/// <summary>
/// How a shape is painted: an optional border and an optional fill.
/// </summary>
/// <remarks>
/// A <c>null</c> fill means "border only" — the challenge's <c>filled: false</c>. Keeping the
/// two together lets <see cref="RenderContext.AppearanceFor"/> express that rule once for
/// every primitive, present and future, instead of repeating it in each renderer.
/// </remarks>
public readonly record struct Appearance(Stroke? Stroke, ArgbColor? Fill);

/// <summary>
/// A single drawing instruction in <b>screen</b> coordinates — the boundary between the
/// UI-independent core and a rendering back end.
/// </summary>
/// <remarks>
/// This vocabulary is deliberately small and closed, while the set of primitives is open:
/// three shapes already express lines, rectangles, polygons and circles, and a renderer may
/// emit several commands to compose one primitive. That asymmetry is what keeps a back end
/// (see the WPF <c>DrawCommandPainter</c>) tiny and stable as primitives are added.
/// </remarks>
public abstract record DrawCommand(Appearance Appearance);

/// <summary>A straight segment.</summary>
public sealed record DrawLine(ScreenPoint Start, ScreenPoint End, Appearance Appearance)
    : DrawCommand(Appearance);

/// <summary>
/// An axis-aligned ellipse. A world circle maps to equal radii because the scale is uniform;
/// the type stays general so a future ellipse primitive needs no new command.
/// </summary>
public sealed record DrawEllipse(
    ScreenPoint Center,
    double RadiusX,
    double RadiusY,
    Appearance Appearance) : DrawCommand(Appearance);

/// <summary>A closed polygon through the given vertices.</summary>
public sealed record DrawPolygon(IReadOnlyList<ScreenPoint> Points, Appearance Appearance)
    : DrawCommand(Appearance)
{
    /// <remarks>
    /// Renderers naturally build vertices in an array, which a consumer could cast back to
    /// and edit. The command normalises the collection itself rather than trusting every
    /// present and future renderer to remember to wrap it. The wrapper is a view, not a copy:
    /// this runs once per shape per frame, and the caller hands over its only reference.
    /// </remarks>
    public IReadOnlyList<ScreenPoint> Points { get; } = Points switch
    {
        ScreenPoint[] array => Array.AsReadOnly(array),
        List<ScreenPoint> list => list.AsReadOnly(),
        _ => Points,
    };
}
