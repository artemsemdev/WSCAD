using FluentAssertions;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain.Geometry;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// The behaviour of the viewport transform is the part of this application most likely to
/// contain a subtle bug, and the part that would be most painful to verify through a UI.
/// These tests are deterministic arithmetic with no WPF anywhere.
/// </summary>
public class ViewportTransformTests
{
    /// <summary>Padding is excluded in most tests so the expected numbers stay hand-checkable.</summary>
    private static readonly ViewportFitOptions NoPadding = new(Padding: 0);

    public class YAxisAndOrigin
    {
        [Fact]
        public void The_identity_transform_maps_the_world_origin_to_the_screen_origin()
        {
            ViewportTransform.Identity.ToScreen(Point2D.Origin).ShouldBe(0, 0);
        }

        [Fact]
        public void Y_is_inverted_because_the_world_points_up_and_the_screen_points_down()
        {
            var transform = ViewportTransform.Identity;

            transform.ToScreen(new Point2D(10, 5)).ShouldBe(10, -5);
            transform.ToScreen(new Point2D(10, -5)).ShouldBe(10, 5);
        }

        [Fact]
        public void A_higher_world_point_lands_further_up_the_screen()
        {
            var transform = ViewportTransform.Fit(new BoundingBox(-5, -5, 5, 5), new ViewportSize(800, 600));

            var low = transform.ToScreen(new Point2D(0, -5));
            var high = transform.ToScreen(new Point2D(0, 5));

            high.Y.Should().BeLessThan(low.Y);
        }

        [Fact]
        public void The_centre_of_the_scene_lands_at_the_centre_of_the_viewport()
        {
            var bounds = new BoundingBox(-5, -5, 5, 5);

            var transform = ViewportTransform.Fit(bounds, new ViewportSize(800, 600));

            transform.ToScreen(bounds.Center).ShouldBe(400, 300);
        }
    }

    public class ScaleToFit
    {
        [Fact]
        public void A_drawing_smaller_than_the_viewport_is_shown_at_100_percent()
        {
            // 10x10 units in an 800x600 window would fit 58x over — but 100 % zoom means
            // 1 unit = 1 pixel, so it must not be magnified.
            var transform = ViewportTransform.Fit(new BoundingBox(-5, -5, 5, 5), new ViewportSize(800, 600));

            transform.Scale.Should().Be(1.0);
            transform.ScalePercentage.Should().Be(100.0);
        }

        [Fact]
        public void At_100_percent_one_world_unit_is_exactly_one_pixel()
        {
            var transform = ViewportTransform.Fit(new BoundingBox(-5, -5, 5, 5), new ViewportSize(800, 600));

            var origin = transform.ToScreen(Point2D.Origin);
            var oneUnitRight = transform.ToScreen(new Point2D(1, 0));
            var oneUnitUp = transform.ToScreen(new Point2D(0, 1));

            (oneUnitRight.X - origin.X).Should().BeApproximately(1.0, 1e-9);
            (origin.Y - oneUnitUp.Y).Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        public void A_drawing_larger_than_the_viewport_is_scaled_down_to_fit()
        {
            // 1000x100 world into a 500x400 viewport: width is the binding constraint.
            var transform = ViewportTransform.Fit(
                new BoundingBox(0, 0, 1000, 100), new ViewportSize(500, 400), NoPadding);

            transform.Scale.Should().Be(0.5);
        }

        [Fact]
        public void Upscaling_is_opt_in()
        {
            var bounds = new BoundingBox(-5, -5, 5, 5);
            var viewport = new ViewportSize(800, 600);

            ViewportTransform.Fit(bounds, viewport, NoPadding).Scale.Should().Be(1.0);
            ViewportTransform.Fit(bounds, viewport, new ViewportFitOptions(0, AllowUpscale: true))
                .Scale.Should().Be(60.0);  // 600 / 10, the binding axis
        }

        [Fact]
        public void The_same_factor_is_applied_to_both_axes_so_the_aspect_ratio_is_preserved()
        {
            var transform = ViewportTransform.Fit(
                new BoundingBox(0, 0, 1000, 100), new ViewportSize(500, 400), NoPadding);

            var horizontal = transform.ToScreen(new Point2D(10, 0)).X - transform.ToScreen(Point2D.Origin).X;
            var vertical = transform.ToScreen(Point2D.Origin).Y - transform.ToScreen(new Point2D(0, 10)).Y;

            horizontal.Should().BeApproximately(vertical, 1e-9);
        }

        [Fact]
        public void A_wide_scene_in_a_narrow_viewport_is_limited_by_width()
        {
            var bounds = new BoundingBox(0, 0, 1000, 10);

            var transform = ViewportTransform.Fit(bounds, new ViewportSize(100, 1000), NoPadding);

            transform.Scale.Should().Be(0.1);
            (transform.ToScreen(new Point2D(1000, 0)).X - transform.ToScreen(Point2D.Origin).X)
                .Should().BeApproximately(100, 1e-9);
        }

        [Fact]
        public void A_tall_scene_in_a_wide_viewport_is_limited_by_height()
        {
            var bounds = new BoundingBox(0, 0, 10, 1000);

            var transform = ViewportTransform.Fit(bounds, new ViewportSize(1000, 100), NoPadding);

            transform.Scale.Should().Be(0.1);
            (transform.ToScreen(Point2D.Origin).Y - transform.ToScreen(new Point2D(0, 1000)).Y)
                .Should().BeApproximately(100, 1e-9);
        }

        [Theory]
        [InlineData(400, 300)]
        [InlineData(120, 900)]
        [InlineData(1600, 200)]
        public void Every_corner_of_the_scene_lands_inside_the_viewport(double width, double height)
        {
            var bounds = new BoundingBox(-320, -240, 680, 760);
            var viewport = new ViewportSize(width, height);

            var transform = ViewportTransform.Fit(bounds, viewport);

            foreach (var corner in new[]
                     {
                         new Point2D(bounds.MinX, bounds.MinY), new Point2D(bounds.MaxX, bounds.MinY),
                         new Point2D(bounds.MinX, bounds.MaxY), new Point2D(bounds.MaxX, bounds.MaxY),
                     })
            {
                var screen = transform.ToScreen(corner);
                screen.X.Should().BeInRange(0, width);
                screen.Y.Should().BeInRange(0, height);
            }
        }
    }

    public class Centring
    {
        [Fact]
        public void A_scene_entirely_in_negative_space_is_centred_like_any_other()
        {
            var bounds = new BoundingBox(-100, -100, -50, -50);

            var transform = ViewportTransform.Fit(bounds, new ViewportSize(200, 200), NoPadding);

            transform.Scale.Should().Be(1.0);            // small scene: no magnification
            transform.ToScreen(new Point2D(-75, -75)).ShouldBe(100, 100);   // centre → centre
            transform.ToScreen(new Point2D(-50, -50)).ShouldBe(125, 75);    // right and up
        }

        [Fact]
        public void A_scene_crossing_the_origin_is_centred_on_its_bounds_not_on_the_origin()
        {
            // Deliberately asymmetric about the origin: the drawing, not (0,0), is centred.
            var bounds = new BoundingBox(-10, -10, 90, 90);

            var transform = ViewportTransform.Fit(bounds, new ViewportSize(200, 200), NoPadding);

            transform.ToScreen(new Point2D(40, 40)).ShouldBe(100, 100);
            transform.ToScreen(Point2D.Origin).ShouldBe(60, 140);
        }

        [Fact]
        public void Padding_is_reserved_on_every_side()
        {
            // With upscaling enabled the drawing exactly fills the padded area, which makes
            // the reserved margin directly observable.
            var transform = ViewportTransform.Fit(
                new BoundingBox(-50, -50, 50, 50),
                new ViewportSize(200, 200),
                new ViewportFitOptions(Padding: 10, AllowUpscale: true));

            transform.Scale.Should().Be(1.8);                            // 180 / 100
            transform.ToScreen(new Point2D(-50, 50)).ShouldBe(10, 10);   // top-left corner
            transform.ToScreen(new Point2D(50, -50)).ShouldBe(190, 190); // bottom-right corner
        }

        [Fact]
        public void Padding_never_consumes_more_than_half_of_a_small_viewport()
        {
            // 8px of padding per side cannot fit in a 10px viewport. Padding adapts instead
            // of collapsing the drawing to zero size.
            var transform = ViewportTransform.Fit(
                new BoundingBox(-5, -5, 5, 5), new ViewportSize(10, 10), ViewportFitOptions.Default);

            transform.Scale.Should().Be(0.5);   // padding capped at 2.5/side ⇒ 5px for 10 units
        }
    }

    public class DegenerateInput
    {
        [Fact]
        public void A_single_point_scene_gets_a_usable_transform_instead_of_a_division_by_zero()
        {
            var transform = ViewportTransform.Fit(
                new BoundingBox(5, 5, 5, 5), new ViewportSize(800, 600));

            transform.Scale.Should().Be(1.0);
            transform.ToScreen(new Point2D(5, 5)).ShouldBe(400, 300);
        }

        [Fact]
        public void A_zero_height_scene_is_scaled_by_its_width_alone()
        {
            var bounds = new BoundingBox(-50, 0, 50, 0);

            ViewportTransform.Fit(bounds, new ViewportSize(50, 100), NoPadding).Scale.Should().Be(0.5);
            ViewportTransform.Fit(bounds, new ViewportSize(400, 100), NoPadding).Scale.Should().Be(1.0);
        }

        [Fact]
        public void A_zero_width_scene_is_scaled_by_its_height_alone()
        {
            var bounds = new BoundingBox(0, -50, 0, 50);

            ViewportTransform.Fit(bounds, new ViewportSize(100, 50), NoPadding).Scale.Should().Be(0.5);
        }

        [Theory]
        [InlineData(0, 600)]
        [InlineData(800, 0)]
        [InlineData(-10, -10)]
        public void An_empty_viewport_never_yields_NaN_or_infinity(double width, double height)
        {
            var transform = ViewportTransform.Fit(
                new BoundingBox(-5, -5, 5, 5), new ViewportSize(width, height));

            transform.Scale.Should().BeGreaterThan(0);
            var screen = transform.ToScreen(Point2D.Origin);
            double.IsFinite(screen.X).Should().BeTrue();
            double.IsFinite(screen.Y).Should().BeTrue();
        }
    }

    public class Lengths
    {
        [Fact]
        public void World_lengths_convert_to_pixels_by_the_scale_factor()
        {
            var transform = ViewportTransform.Fit(
                new BoundingBox(0, 0, 1000, 100), new ViewportSize(500, 400), NoPadding);

            transform.ToScreenLength(10).Should().Be(5);
        }

        [Fact]
        public void Pixel_lengths_convert_back_to_world_units()
        {
            // A future hit-test tolerance is expressed in pixels and must become world units.
            var transform = ViewportTransform.Fit(
                new BoundingBox(0, 0, 1000, 100), new ViewportSize(500, 400), NoPadding);

            transform.ToWorldLength(5).Should().Be(10);
        }
    }

    public class Inversion
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(-15, 21)]
        [InlineData(2.2, -5.7)]
        public void ToWorld_undoes_ToScreen(double x, double y)
        {
            var transform = ViewportTransform.Fit(
                new BoundingBox(-320, -240, 680, 760), new ViewportSize(1024, 768));
            var world = new Point2D(x, y);

            transform.ToWorld(transform.ToScreen(world)).ShouldBe(x, y);
        }

        [Fact]
        public void The_viewport_centre_maps_back_to_the_scene_centre()
        {
            var bounds = new BoundingBox(-10, -10, 90, 90);
            var transform = ViewportTransform.Fit(bounds, new ViewportSize(200, 200), NoPadding);

            transform.ToWorld(new ScreenPoint(100, 100)).ShouldBe(bounds.Center.X, bounds.Center.Y);
        }
    }
}

public class ViewportFitOptionsTests
{
    [Fact]
    public void The_default_policy_caps_the_scale_at_100_percent()
    {
        ViewportFitOptions.Default.MaximumScale.Should().Be(1.0);
        ViewportFitOptions.Default.AllowUpscale.Should().BeFalse();
    }

    [Fact]
    public void Allowing_upscale_removes_the_cap()
    {
        new ViewportFitOptions(AllowUpscale: true).MaximumScale.Should().Be(double.PositiveInfinity);
    }
}
