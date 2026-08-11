using FluentAssertions;
using Xunit;

namespace VectorViewer.Domain.Tests;

public class ArgbColorTests
{
    [Fact]
    public void Channels_are_stored_in_alpha_red_green_blue_order()
    {
        // The challenge's half-transparent red: "127; 255; 0; 0".
        var color = new ArgbColor(127, 255, 0, 0);

        color.A.Should().Be(127);
        color.R.Should().Be(255);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void ToUInt32_packs_the_channels_as_AARRGGBB()
    {
        new ArgbColor(127, 255, 0, 255).ToUInt32().Should().Be(0x7FFF00FFu);
    }

    [Fact]
    public void FromUInt32_round_trips()
    {
        var color = new ArgbColor(12, 34, 56, 78);

        ArgbColor.FromUInt32(color.ToUInt32()).Should().Be(color);
    }

    [Fact]
    public void A_zero_alpha_colour_is_fully_transparent()
    {
        new ArgbColor(0, 255, 255, 255).IsFullyTransparent.Should().BeTrue();
        new ArgbColor(1, 0, 0, 0).IsFullyTransparent.Should().BeFalse();
    }

    [Fact]
    public void Colours_with_the_same_channels_are_equal()
    {
        new ArgbColor(127, 255, 0, 0).Should().Be(new ArgbColor(127, 255, 0, 0));
        new ArgbColor(127, 255, 0, 0).Should().NotBe(new ArgbColor(255, 255, 0, 0));
    }
}
