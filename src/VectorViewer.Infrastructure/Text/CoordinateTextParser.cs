using System.Globalization;
using VectorViewer.Domain.Geometry;

namespace VectorViewer.Infrastructure.Text;

/// <summary>
/// Parses the challenge's textual coordinate format, e.g. <c>"-1,5; 3,4"</c> ⇒ <c>(-1.5, 3.4)</c>.
/// </summary>
/// <remarks>
/// <para>
/// In the input format <c>;</c> separates the components and <c>,</c> is the decimal separator.
/// (Confirmed by <c>"15; -20,3"</c> in the sample, and by a line needing exactly two endpoints.)
/// </para>
/// <para>
/// Both <c>,</c> and <c>.</c> are accepted as decimal separators and parsing is explicitly
/// culture-independent, so the viewer behaves identically on a German and an English machine —
/// a common source of production bugs when files move between locales. Thousands separators
/// are assumed absent, which is consistent with the format.
/// </para>
/// <para>Lives in its own namespace because it is format-agnostic: an XML reader would reuse it.</para>
/// </remarks>
public static class CoordinateTextParser
{
    /// <summary>Only a sign, digits and a single decimal separator — no thousands grouping.</summary>
    private const NumberStyles NumberFormat = NumberStyles.Float;

    /// <summary>Parses a single number, accepting either a comma or a dot as the decimal separator.</summary>
    public static double ParseNumber(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        var normalised = trimmed.Contains(',', StringComparison.Ordinal)
            ? trimmed.Replace(',', '.')
            : trimmed;

        return double.Parse(normalised, NumberFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>Parses a <c>"x; y"</c> pair.</summary>
    /// <exception cref="FormatException">The text does not contain exactly two components.</exception>
    public static Point2D ParsePoint(string text)
    {
        var components = SplitComponents(text);
        if (components.Length != 2)
        {
            throw new FormatException(
                $"A point needs exactly two components separated by ';', but '{text}' has {components.Length}.");
        }

        return new Point2D(ParseNumber(components[0]), ParseNumber(components[1]));
    }

    /// <summary>Splits a semicolon-separated list into trimmed, non-empty components.</summary>
    public static string[] SplitComponents(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
