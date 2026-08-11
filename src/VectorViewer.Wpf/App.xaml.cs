using System.IO;
using System.Windows;
using VectorViewer.Wpf.ViewModels;

namespace VectorViewer.Wpf;

public partial class App : System.Windows.Application
{
    /// <summary>The challenge sample, copied next to the executable at build time.</summary>
    private static string DefaultDrawingPath =>
        Path.Combine(AppContext.BaseDirectory, "samples", "example.json");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = CompositionRoot.CreateMainViewModel();

        // A path may be passed on the command line; otherwise show the bundled sample so the
        // window is never empty on first run.
        var initialPath = e.Args.FirstOrDefault() ?? DefaultDrawingPath;
        if (File.Exists(initialPath))
        {
            viewModel.Load(initialPath);
        }

        MainWindow = new MainWindow { DataContext = viewModel };
        MainWindow.Show();
    }
}
