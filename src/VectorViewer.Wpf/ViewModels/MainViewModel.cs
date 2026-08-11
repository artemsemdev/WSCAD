using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VectorViewer.Application.Documents;
using VectorViewer.Domain;
using VectorViewer.Wpf.Services;

namespace VectorViewer.Wpf.ViewModels;

/// <summary>
/// Presentation state for the main window: the loaded scene, the current zoom and a status line.
/// </summary>
/// <remarks>
/// The view model orchestrates loading and holds state; it contains no geometry and no drawing
/// code. Rendering is the <c>SceneView</c>'s job, and the maths belongs to the application layer.
/// </remarks>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly VectorDocumentLoader _loader;
    private readonly IFileDialogService _fileDialog;

    private Scene? _scene;
    private string _statusText = "No drawing loaded.";
    private string _title = "Vector Graphic Viewer";
    private double _zoomPercentage = 100;

    public MainViewModel(VectorDocumentLoader loader, IFileDialogService fileDialog)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _fileDialog = fileDialog ?? throw new ArgumentNullException(nameof(fileDialog));

        OpenCommand = new RelayCommand(Open);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand OpenCommand { get; }

    public Scene? Scene
    {
        get => _scene;
        private set => Set(ref _scene, value);
    }

    /// <summary>Bound <c>OneWayToSource</c> from the view, which is the only place the fit is known.</summary>
    public double ZoomPercentage
    {
        get => _zoomPercentage;
        set
        {
            if (Set(ref _zoomPercentage, value))
            {
                OnPropertyChanged(nameof(ZoomText));
            }
        }
    }

    public string ZoomText => string.Create(CultureInfo.CurrentCulture, $"Zoom: {_zoomPercentage:0.#} %");

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public string Title
    {
        get => _title;
        private set => Set(ref _title, value);
    }

    /// <summary>Loads a file, reporting failures in the status bar rather than crashing the app.</summary>
    public void Load(string path)
    {
        try
        {
            Scene = _loader.Load(path);
            Title = $"Vector Graphic Viewer — {Path.GetFileName(path)}";
            StatusText = Describe(Scene);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                              or NotSupportedException or FormatException
                                              or System.Text.Json.JsonException)
        {
            // The challenge guarantees valid input, so this is about surviving a mis-picked
            // file rather than validating the format.
            Scene = null;
            StatusText = $"Could not load '{Path.GetFileName(path)}': {exception.Message}";
        }
    }

    /// <summary>
    /// The file dialog filter, derived from the readers that are actually registered — one
    /// entry per format, so adding an XML reader adds its entry with no change here.
    /// </summary>
    private string BuildFileFilter()
    {
        var entries = _loader.Readers.Select(reader =>
        {
            var patterns = string.Join(";", reader.SupportedExtensions.Select(extension => $"*{extension}"));
            return $"{reader.FormatName} ({patterns})|{patterns}";
        });

        return string.Join("|", entries.Append("All files (*.*)|*.*"));
    }

    private static string Describe(Scene scene)
    {
        if (scene.Bounds is not { } bounds)
        {
            return "Loaded an empty drawing.";
        }

        return string.Create(
            CultureInfo.CurrentCulture,
            $"{scene.Primitives.Count} primitives — " +
            $"bounds ({bounds.MinX:0.##}; {bounds.MinY:0.##}) to ({bounds.MaxX:0.##}; {bounds.MaxY:0.##}), " +
            $"{bounds.Width:0.##} × {bounds.Height:0.##} units");
    }

    private void Open()
    {
        if (_fileDialog.PickFileToOpen(BuildFileFilter()) is { } path)
        {
            Load(path);
        }
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
