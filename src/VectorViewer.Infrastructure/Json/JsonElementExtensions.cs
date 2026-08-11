using System.Text.Json;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Infrastructure.Text;

namespace VectorViewer.Infrastructure.Json;

/// <summary>
/// Reading helpers shared by the built-in mappers, keeping each mapper to a single expression.
/// </summary>
/// <remarks>
/// Internal on purpose: a mapper in another assembly composes the public
/// <see cref="CoordinateTextParser"/> and <see cref="ArgbColorParser"/> instead, which keeps
/// the supported extension surface small and stable.
/// </remarks>
internal static class JsonElementExtensions
{
    public static Point2D ReadPoint(this JsonElement element, string propertyName) =>
        CoordinateTextParser.ParsePoint(element.GetProperty(propertyName).GetString()
            ?? throw new FormatException($"Property '{propertyName}' must be a coordinate string."));

    public static ArgbColor ReadColor(this JsonElement element) =>
        ArgbColorParser.Parse(element.GetProperty("color").GetString()
            ?? throw new FormatException("Property 'color' must be an 'A; R; G; B' string."));

    /// <summary>Reads the optional <c>filled</c> flag; absent means "border only".</summary>
    public static bool ReadFilled(this JsonElement element) =>
        element.TryGetProperty("filled", out var filled) && filled.GetBoolean();
}
