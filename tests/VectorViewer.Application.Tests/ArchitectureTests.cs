using FluentAssertions;
using VectorViewer.Application.Rendering;
using VectorViewer.Domain;
using Xunit;

namespace VectorViewer.Application.Tests;

/// <summary>
/// Guards the boundary the whole design rests on. Documentation drifts; this does not.
/// </summary>
public class ArchitectureTests
{
    /// <summary>Assemblies that would drag a UI framework into the core.</summary>
    private static readonly string[] ForbiddenAssemblies =
        ["PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms"];

    [Fact]
    public void The_domain_does_not_depend_on_a_ui_framework()
    {
        ReferencedAssemblyNames(typeof(Scene)).Should().NotIntersectWith(ForbiddenAssemblies);
    }

    [Fact]
    public void The_application_layer_does_not_depend_on_a_ui_framework()
    {
        // If this ever fails, the geometry and scaling logic has stopped being testable
        // without a UI thread — the single most valuable property of this architecture.
        ReferencedAssemblyNames(typeof(SceneRenderer)).Should().NotIntersectWith(ForbiddenAssemblies);
    }

    [Fact]
    public void The_domain_does_not_depend_on_the_application_layer()
    {
        // Dependencies point inwards only; the domain is the innermost layer.
        ReferencedAssemblyNames(typeof(Scene)).Should().NotContain("VectorViewer.Application");
    }

    private static IEnumerable<string> ReferencedAssemblyNames(Type typeInAssembly) =>
        typeInAssembly.Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name!);
}
