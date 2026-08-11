using System.Globalization;
using VectorViewer.Domain;

namespace VectorViewer.Infrastructure.Text;

/// <summary>
/// Parses the textual colour format <c>"A; R; G; B"</c>, e.g. <c>"127; 255; 0; 0"</c> —
/// a half-transparent red. Alpha comes first, as specified by the challenge.
/// </summary>
public static class ArgbColorParser
{
    /// <exception cref="FormatException">The text does not contain exactly four channels.</exception>
    public static ArgbColor Parse(string text)
    {
        var channels = CoordinateTextParser.SplitComponents(text);
        if (channels.Length != 4)
        {
            throw new FormatException(
                $"A colour needs exactly four ARGB channels, but '{text}' has {channels.Length}.");
        }

        return new ArgbColor(
            ParseChannel(channels[0]),
            ParseChannel(channels[1]),
            ParseChannel(channels[2]),
            ParseChannel(channels[3]));
    }

    private static byte ParseChannel(string text) =>
        byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
