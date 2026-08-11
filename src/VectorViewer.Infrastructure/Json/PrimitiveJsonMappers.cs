using System.Text.Json;
using VectorViewer.Domain.Primitives;

namespace VectorViewer.Infrastructure.Json;

/// <summary>Maps <c>{ "type": "line", "a": "x; y", "b": "x; y", "color": "a; r; g; b" }</c>.</summary>
public sealed class LineJsonMapper : IPrimitiveJsonMapper
{
    public string TypeDiscriminator => "line";

    public IPrimitive Map(JsonElement element) => new Line(
        element.ReadPoint("a"),
        element.ReadPoint("b"),
        element.ReadColor());
}

/// <summary>Maps <c>{ "type": "circle", "center": "x; y", "radius": 15.0, "filled": false, ... }</c>.</summary>
public sealed class CircleJsonMapper : IPrimitiveJsonMapper
{
    public string TypeDiscriminator => "circle";

    public IPrimitive Map(JsonElement element) => new Circle(
        element.ReadPoint("center"),
        // The radius is a JSON number, so it follows the JSON grammar (dot) rather than the
        // comma convention used inside the quoted coordinate strings.
        element.GetProperty("radius").GetDouble(),
        element.ReadColor(),
        element.ReadFilled());
}

/// <summary>Maps <c>{ "type": "triangle", "a": …, "b": …, "c": …, "filled": true, ... }</c>.</summary>
public sealed class TriangleJsonMapper : IPrimitiveJsonMapper
{
    public string TypeDiscriminator => "triangle";

    public IPrimitive Map(JsonElement element) => new Triangle(
        element.ReadPoint("a"),
        element.ReadPoint("b"),
        element.ReadPoint("c"),
        element.ReadColor(),
        element.ReadFilled());
}
