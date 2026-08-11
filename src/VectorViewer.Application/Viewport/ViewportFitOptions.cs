namespace VectorViewer.Application.Viewport;

/// <summary>
/// Policy for fitting a scene into a viewport.
/// </summary>
/// <param name="Padding">
/// Pixels reserved on every side. Scene bounds cover geometry only, so without padding a
/// border stroke on a shape at the edge of the drawing would be clipped by the window.
/// </param>
/// <param name="AllowUpscale">
/// When false (the default) a drawing smaller than the viewport is shown at 100 % —
/// one world unit per pixel — instead of being magnified, as the challenge specifies.
/// </param>
public sealed record ViewportFitOptions(double Padding = 8.0, bool AllowUpscale = false)
{
    public static ViewportFitOptions Default { get; } = new();

    /// <summary>The largest permitted scale factor: 1.0 (100 % zoom) unless upscaling is allowed.</summary>
    public double MaximumScale => AllowUpscale ? double.PositiveInfinity : 1.0;
}
