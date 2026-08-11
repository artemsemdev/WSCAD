using System.Diagnostics.CodeAnalysis;
using VectorViewer.Application.Rendering.Renderers;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Application.Rendering;

/// <summary>
/// Maps a primitive type to the renderer that draws it.
/// </summary>
/// <remarks>
/// <para>
/// This replaces the <c>switch</c> over primitive types that such a viewer usually grows.
/// Adding a primitive is a registration, not a modification — the Open/Closed Principle
/// applied to the axis of change the challenge actually predicts.
/// </para>
/// <para>
/// A visitor was considered and rejected: it makes adding an <i>operation</i> cheap and
/// adding a <i>type</i> expensive, which is the wrong way round here.
/// </para>
/// </remarks>
public sealed class PrimitiveRendererRegistry
{
    private readonly Dictionary<Type, IPrimitiveRenderer> _renderers = [];

    /// <summary>A registry containing the renderers for the built-in primitives.</summary>
    public static PrimitiveRendererRegistry CreateDefault() => new PrimitiveRendererRegistry()
        .Register(new LineRenderer())
        .Register(new CircleRenderer())
        .Register(new TriangleRenderer());

    /// <summary>
    /// Registers a renderer, replacing any renderer previously registered for the same
    /// primitive type so a host can override built-in appearance. Returns this instance
    /// so registrations can be chained.
    /// </summary>
    public PrimitiveRendererRegistry Register(IPrimitiveRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        _renderers[renderer.PrimitiveType] = renderer;
        return this;
    }

    public bool TryGetRenderer(Type primitiveType, [NotNullWhen(true)] out IPrimitiveRenderer? renderer) =>
        _renderers.TryGetValue(primitiveType, out renderer);

    /// <summary>Resolves the renderer for a primitive, or throws with a message naming the missing type.</summary>
    /// <exception cref="NotSupportedException">No renderer is registered for the primitive's type.</exception>
    public IPrimitiveRenderer GetRenderer(IPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);

        var type = primitive.GetType();
        if (TryGetRenderer(type, out var renderer))
        {
            return renderer;
        }

        // Failing loudly beats silently omitting a shape: a drawing that is quietly wrong is
        // far harder to diagnose than one that reports the primitive it cannot draw.
        throw new NotSupportedException(
            $"No renderer is registered for primitive type '{type.Name}'. " +
            $"Register one with {nameof(PrimitiveRendererRegistry)}.{nameof(Register)}.");
    }
}
