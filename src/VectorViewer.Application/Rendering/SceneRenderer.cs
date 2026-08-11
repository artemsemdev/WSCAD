using VectorViewer.Application.Viewport;
using VectorViewer.Domain;

namespace VectorViewer.Application.Rendering;

/// <summary>The draw commands for one scene plus the transform that produced them.</summary>
/// <remarks>The transform is returned so the UI can report the zoom level and, later, hit-test.</remarks>
public sealed record RenderedScene(IReadOnlyList<DrawCommand> Commands, ViewportTransform Transform)
{
    public static RenderedScene Empty { get; } = new([], ViewportTransform.Identity);

    public double Scale => Transform.Scale;
}

/// <summary>
/// Turns a scene into draw commands: computes the fit-to-viewport transform from the scene
/// bounds, then delegates each primitive to its registered renderer.
/// </summary>
/// <remarks>
/// This is the whole orchestration of a redraw. It is cheap and allocation-light on purpose:
/// it runs on every window resize, whereas parsing happens once per file.
/// </remarks>
public sealed class SceneRenderer(PrimitiveRendererRegistry registry, RenderOptions? options = null)
{
    private readonly PrimitiveRendererRegistry _registry = registry;
    private readonly RenderOptions _options = options ?? RenderOptions.Default;

    public RenderedScene Render(
        Scene scene,
        ViewportSize viewport,
        ViewportFitOptions? fitOptions = null)
    {
        ArgumentNullException.ThrowIfNull(scene);

        // No primitives means no bounds, and therefore nothing to fit a transform to.
        if (scene.Bounds is not { } bounds)
        {
            return RenderedScene.Empty;
        }

        var transform = ViewportTransform.Fit(bounds, viewport, fitOptions);
        var context = new RenderContext(transform, _options);

        // Most primitives yield exactly one command, so this capacity avoids regrowth.
        var commands = new List<DrawCommand>(scene.Primitives.Count);
        foreach (var primitive in scene.Primitives)
        {
            _registry.GetRenderer(primitive).Render(primitive, context, commands);
        }

        return new RenderedScene(commands, transform);
    }
}
