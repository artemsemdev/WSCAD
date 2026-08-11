using VectorViewer.Domain.Primitives;

namespace VectorViewer.Application.Rendering;

/// <summary>
/// Translates one kind of primitive into draw commands.
/// </summary>
/// <remarks>
/// This is the extension point for new primitives: implement it, register it, done.
/// Commands are appended to a caller-owned collection rather than returned as an
/// enumerable, to keep the per-primitive redraw loop free of intermediate allocations.
/// </remarks>
public interface IPrimitiveRenderer
{
    /// <summary>The exact primitive type this renderer handles; the registry key.</summary>
    Type PrimitiveType { get; }

    /// <summary>Appends the commands that draw <paramref name="primitive"/> to <paramref name="output"/>.</summary>
    /// <exception cref="ArgumentException">The primitive is not of the handled type.</exception>

    void Render(IPrimitive primitive, RenderContext context, ICollection<DrawCommand> output);
}

/// <summary>
/// Base class giving renderers a strongly typed <c>Render</c> and removing the cast boilerplate.
/// </summary>
public abstract class PrimitiveRenderer<TPrimitive> : IPrimitiveRenderer
    where TPrimitive : IPrimitive
{
    public Type PrimitiveType => typeof(TPrimitive);

    void IPrimitiveRenderer.Render(
        IPrimitive primitive,
        RenderContext context,
        ICollection<DrawCommand> output)
    {
        if (primitive is not TPrimitive typed)
        {
            throw new ArgumentException(
                $"{GetType().Name} renders {typeof(TPrimitive).Name}, but was given " +
                $"{primitive?.GetType().Name ?? "null"}.",
                nameof(primitive));
        }

        Render(typed, context, output);
    }

    protected abstract void Render(
        TPrimitive primitive,
        RenderContext context,
        ICollection<DrawCommand> output);
}
