using System.Text;
using System.Text.Json;
using FluentAssertions;
using VectorViewer.Domain;
using VectorViewer.Domain.Geometry;
using VectorViewer.Domain.Primitives;
using VectorViewer.Infrastructure.Json;
using VectorViewer.Infrastructure.Text;
using Xunit;

namespace VectorViewer.Infrastructure.Tests;

public class JsonVectorDocumentReaderTests
{
    private static Scene Read(string json) =>
        new JsonVectorDocumentReader().Read(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    private static T ReadSingle<T>(string json) where T : IPrimitive =>
        Read($"[{json}]").Primitives.Should().ContainSingle().Which.Should().BeOfType<T>().Subject;

    public class Lines
    {
        [Fact]
        public void A_line_is_read_with_both_endpoints_and_its_colour()
        {
            var line = ReadSingle<Line>("""
                { "type": "line", "a": "-1,5; 3,4", "b": "2,2; 5,7", "color": "127; 255; 255; 255" }
                """);

            line.Start.Should().Be(new Point2D(-1.5, 3.4));
            line.End.Should().Be(new Point2D(2.2, 5.7));
            line.Color.Should().Be(new ArgbColor(127, 255, 255, 255));
        }

        [Fact]
        public void Negative_coordinates_are_read_correctly()
        {
            var line = ReadSingle<Line>("""
                { "type": "line", "a": "-100; -200", "b": "-1; -2", "color": "255; 0; 0; 0" }
                """);

            line.Start.Should().Be(new Point2D(-100, -200));
            line.End.Should().Be(new Point2D(-1, -2));
        }
    }

    public class Circles
    {
        [Fact]
        public void A_circle_is_read_with_its_centre_radius_and_fill_flag()
        {
            var circle = ReadSingle<Circle>("""
                { "type": "circle", "center": "0; 0", "radius": 15.0, "filled": false, "color": "127; 255; 0; 0" }
                """);

            circle.Center.Should().Be(Point2D.Origin);
            circle.Radius.Should().Be(15.0);
            circle.Filled.Should().BeFalse();
            circle.Color.Should().Be(new ArgbColor(127, 255, 0, 0));
        }

        [Fact]
        public void A_filled_circle_is_read_as_filled()
        {
            ReadSingle<Circle>("""
                { "type": "circle", "center": "1; 2", "radius": 3, "filled": true, "color": "255; 1; 2; 3" }
                """).Filled.Should().BeTrue();
        }

        [Fact]
        public void A_fractional_radius_is_read_as_a_json_number()
        {
            // radius uses the JSON grammar (dot), unlike the quoted coordinate strings.
            ReadSingle<Circle>("""
                { "type": "circle", "center": "0; 0", "radius": 12.75, "filled": false, "color": "255; 1; 2; 3" }
                """).Radius.Should().Be(12.75);
        }

        [Fact]
        public void A_missing_filled_flag_defaults_to_unfilled()
        {
            ReadSingle<Circle>("""
                { "type": "circle", "center": "0; 0", "radius": 1, "color": "255; 1; 2; 3" }
                """).Filled.Should().BeFalse();
        }
    }

    public class Triangles
    {
        [Fact]
        public void A_triangle_is_read_with_all_three_vertices()
        {
            var triangle = ReadSingle<Triangle>("""
                { "type": "triangle", "a": "-15; -20", "b": "15; -20,3", "c": "0; 21",
                  "filled": true, "color": "127; 255; 0; 255" }
                """);

            triangle.A.Should().Be(new Point2D(-15, -20));
            triangle.B.Should().Be(new Point2D(15, -20.3));
            triangle.C.Should().Be(new Point2D(0, 21));
            triangle.Filled.Should().BeTrue();
            triangle.Color.Should().Be(new ArgbColor(127, 255, 0, 255));
        }
    }

    public class Documents
    {
        [Fact]
        public void Multiple_primitives_of_different_types_are_all_read()
        {
            var scene = Read("""
                [
                  { "type": "line", "a": "0; 0", "b": "1; 1", "color": "255; 1; 1; 1" },
                  { "type": "circle", "center": "0; 0", "radius": 5, "filled": true, "color": "255; 2; 2; 2" },
                  { "type": "triangle", "a": "0; 0", "b": "1; 0", "c": "0; 1", "filled": false, "color": "255; 3; 3; 3" }
                ]
                """);

            scene.Primitives.Should().HaveCount(3);
        }

        [Fact]
        public void Document_order_is_preserved_because_it_is_the_draw_order()
        {
            var scene = Read("""
                [
                  { "type": "circle", "center": "0; 0", "radius": 1, "filled": true, "color": "255; 1; 1; 1" },
                  { "type": "line", "a": "0; 0", "b": "1; 1", "color": "255; 2; 2; 2" },
                  { "type": "circle", "center": "5; 5", "radius": 2, "filled": false, "color": "255; 3; 3; 3" }
                ]
                """);

            scene.Primitives.Select(p => p.GetType())
                .Should().Equal(typeof(Circle), typeof(Line), typeof(Circle));
            scene.Primitives.Select(p => p.Color.R).Should().Equal(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public void An_empty_document_yields_an_empty_scene()
        {
            Read("[]").IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void The_type_discriminator_is_matched_case_insensitively()
        {
            Read("""[{ "type": "CIRCLE", "center": "0; 0", "radius": 1, "color": "255; 1; 1; 1" }]""")
                .Primitives.Should().ContainSingle().Which.Should().BeOfType<Circle>();
        }

        [Fact]
        public void An_unknown_primitive_type_fails_loudly_instead_of_being_skipped()
        {
            // Input is assumed valid, but silently dropping a shape would be a confusing
            // failure mode: the drawing would simply be wrong with no indication why.
            var act = () => Read("""[{ "type": "hexagon", "color": "255; 1; 1; 1" }]""");

            act.Should().Throw<NotSupportedException>().WithMessage("*hexagon*");
        }
    }

    public class FormatRegistration
    {
        [Fact]
        public void The_reader_declares_the_json_extension()
        {
            new JsonVectorDocumentReader().SupportedExtensions.Should().Contain(".json");
        }

        [Fact]
        public void A_custom_mapper_set_can_extend_the_format_with_a_new_primitive()
        {
            // Reading a new primitive type is an added mapper, not a modified switch.
            var reader = new JsonVectorDocumentReader([new LineJsonMapper(), new DotJsonMapper()]);

            var scene = reader.Read(new MemoryStream(Encoding.UTF8.GetBytes(
                """[{ "type": "dot", "at": "3; 4", "color": "255; 9; 9; 9" }]""")));

            scene.Primitives.Should().ContainSingle().Which.Should().BeOfType<Circle>()
                .Which.Center.Should().Be(new Point2D(3, 4));
        }

        /// <summary>A minimal third-party mapper, declared entirely outside the production assembly.</summary>
        private sealed class DotJsonMapper : IPrimitiveJsonMapper
        {
            public string TypeDiscriminator => "dot";

            public IPrimitive Map(JsonElement element) => new Circle(
                Center: CoordinateTextParser.ParsePoint(element.GetProperty("at").GetString()!),
                Radius: 1,
                Color: ArgbColorParser.Parse(element.GetProperty("color").GetString()!),
                Filled: true);
        }
    }
}
