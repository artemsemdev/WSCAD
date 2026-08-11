namespace VectorViewer.Application.Rendering;

/// <summary>
/// Appearance policy shared by all primitive renderers.
/// </summary>
/// <param name="BorderWidthInWorldUnits">
/// Border width in <b>world</b> units, so it scales with the drawing (challenge: "assume
/// arbitrary border width, eg. 1 unit").
/// </param>
/// <param name="MinimumBorderWidthInPixels">
/// Lower bound in device pixels. A heavily scaled-down drawing would otherwise render
/// sub-pixel borders and effectively disappear.
/// </param>
public sealed record RenderOptions(
    double BorderWidthInWorldUnits = 1.0,
    double MinimumBorderWidthInPixels = 1.0)
{
    public static RenderOptions Default { get; } = new();
}
