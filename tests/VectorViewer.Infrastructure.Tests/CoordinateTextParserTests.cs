using System.Globalization;
using FluentAssertions;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Infrastructure.Text;
using Xunit;

namespace VectorViewer.Infrastructure.Tests;

/// <summary>
/// The input encodes a point as <c>"x; y"</c> with a <b>comma</b> as the decimal separator.
/// Getting this wrong silently turns one point into two, so it is tested directly.
/// </summary>
public class CoordinateTextParserTests
{
    [Fact]
    public void A_comma_is_the_decimal_separator_not_a_coordinate_separator()
    {
        // "-1,5; 3,4" is ONE point (-1.5, 3.4) — not two points and not four numbers.
        CoordinateTextParser.ParsePoint("-1,5; 3,4").Should().Be(new Point2D(-1.5, 3.4));
    }

    [Fact]
    public void Whole_numbers_parse_without_a_decimal_separator()
    {
        CoordinateTextParser.ParsePoint("0; 0").Should().Be(Point2D.Origin);
        CoordinateTextParser.ParsePoint("-15; -20").Should().Be(new Point2D(-15, -20));
    }

    [Fact]
    public void A_point_may_mix_whole_and_fractional_coordinates()
    {
        CoordinateTextParser.ParsePoint("15; -20,3").Should().Be(new Point2D(15, -20.3));
    }

    [Fact]
    public void A_dot_is_also_accepted_as_a_decimal_separator()
    {
        // Tolerated so files produced by an invariant-culture writer still load.
        CoordinateTextParser.ParsePoint("-1.5; 3.4").Should().Be(new Point2D(-1.5, 3.4));
    }

    [Theory]
    [InlineData("1;2")]
    [InlineData("1; 2")]
    [InlineData(" 1 ;  2 ")]
    public void Surrounding_whitespace_is_ignored(string text)
    {
        CoordinateTextParser.ParsePoint(text).Should().Be(new Point2D(1, 2));
    }

    [Theory]
    [InlineData("-1,5", -1.5)]
    [InlineData("-1.5", -1.5)]
    [InlineData("15", 15)]
    [InlineData("0", 0)]
    [InlineData("-0,000125", -0.000125)]
    public void Numbers_parse_with_either_decimal_separator(string text, double expected)
    {
        CoordinateTextParser.ParseNumber(text).Should().Be(expected);
    }

    [Theory]
    [InlineData("de-DE")]   // decimal comma, thousands dot
    [InlineData("en-US")]   // decimal dot, thousands comma
    [InlineData("fr-FR")]   // decimal comma, narrow-nbsp thousands
    public void Parsing_does_not_depend_on_the_current_culture(string culture)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            // The same file must produce the same drawing on every machine.
            CoordinateTextParser.ParsePoint("-1,5; 3,4").Should().Be(new Point2D(-1.5, 3.4));
            CoordinateTextParser.ParsePoint("-1.5; 3.4").Should().Be(new Point2D(-1.5, 3.4));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Components_are_split_on_semicolons_and_trimmed()
    {
        CoordinateTextParser.SplitComponents("127; 255; 0; 0")
            .Should().Equal("127", "255", "0", "0");
    }
}

public class ArgbColorParserTests
{
    [Fact]
    public void Channels_are_read_in_alpha_red_green_blue_order()
    {
        ArgbColorParser.Parse("127; 255; 0; 0").Should().Be(new ArgbColor(127, 255, 0, 0));
    }

    [Theory]
    [InlineData("0; 0; 0; 0", 0, 0, 0, 0)]
    [InlineData("255; 255; 255; 255", 255, 255, 255, 255)]
    [InlineData("127; 255; 0; 255", 127, 255, 0, 255)]
    public void The_full_channel_range_is_supported(string text, byte a, byte r, byte g, byte b)
    {
        ArgbColorParser.Parse(text).Should().Be(new ArgbColor(a, r, g, b));
    }

    [Fact]
    public void Whitespace_around_channels_is_ignored()
    {
        ArgbColorParser.Parse("127;255;0;0").Should().Be(new ArgbColor(127, 255, 0, 0));
    }

    [Fact]
    public void Alpha_is_preserved_rather_than_forced_opaque()
    {
        // Every colour in the challenge sample is half-transparent; dropping alpha would be
        // visually obvious but easy to miss in code.
        ArgbColorParser.Parse("127; 255; 0; 0").A.Should().Be(127);
    }
}
