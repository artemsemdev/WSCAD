using VectorViewer.Application.Documents;
using VectorViewer.Infrastructure.Json;
using VectorViewer.Wpf.Services;
using VectorViewer.Wpf.ViewModels;

namespace VectorViewer.Wpf;

/// <summary>
/// Builds the object graph. This is the single place where concrete adapters are chosen.
/// </summary>
/// <remarks>
/// A DI container was considered and rejected: the graph is a handful of objects with no
/// lifetimes to manage, so a container would add indirection and configuration without
/// removing any wiring. Adding an XML reader means one more entry in the list below.
/// </remarks>
internal static class CompositionRoot
{
    public static MainViewModel CreateMainViewModel() => new(
        new VectorDocumentLoader([new JsonVectorDocumentReader()]),
        new FileDialogService());
}
