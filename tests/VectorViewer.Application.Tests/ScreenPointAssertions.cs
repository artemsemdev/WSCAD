using FluentAssertions;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain.Geometry;

namespace VectorViewer.Application.Tests;

/// <summary>
/// Coordinate assertions with a tolerance, so tests state exact expected geometry without
/// being hostage to floating-point representation.
/// </summary>
internal static class ScreenPointAssertions
{
    private const double Tolerance = 1e-9;

    public static void ShouldBe(this ScreenPoint actual, double x, double y)
    {
        actual.X.Should().BeApproximately(x, Tolerance, "X of {0}", actual);
        actual.Y.Should().BeApproximately(y, Tolerance, "Y of {0}", actual);
    }

    public static void ShouldBe(this Point2D actual, double x, double y)
    {
        actual.X.Should().BeApproximately(x, Tolerance, "X of {0}", actual);
        actual.Y.Should().BeApproximately(y, Tolerance, "Y of {0}", actual);
    }
}
