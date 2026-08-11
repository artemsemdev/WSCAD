using System.Text.Json;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Infrastructure.Json;

/// <summary>
/// Maps one JSON object shape — identified by its <c>"type"</c> discriminator — to a primitive.
/// </summary>
/// <remarks>
/// The same open/closed idea as the renderer registry, one level down: reading a new primitive
/// from JSON means adding a mapper, not extending a <c>switch</c>. Hand-written mapping is
/// preferred over <c>System.Text.Json</c> polymorphic attributes because the input uses a custom
/// textual encoding for points and colours that no built-in converter understands anyway.
/// </remarks>
public interface IPrimitiveJsonMapper
{
    /// <summary>The <c>"type"</c> value this mapper handles, e.g. <c>"line"</c>. Matched case-insensitively.</summary>
    string TypeDiscriminator { get; }

    IPrimitive Map(JsonElement element);
}
