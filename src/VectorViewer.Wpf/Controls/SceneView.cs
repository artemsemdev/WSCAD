using System.Windows;
using System.Windows.Media;
using VectorViewer.Application.Rendering;
using VectorViewer.Application.Viewport;
using VectorViewer.Domain;

namespace VectorViewer.Wpf.Controls;

/// <summary>
/// Displays a <see cref="Scene"/>, refitting it whenever the scene or the control size changes.
/// </summary>
/// <remarks>
/// <para>
/// A single <see cref="OnRender"/> pass rather than one <c>Shape</c> element per primitive:
/// the visual tree stays flat regardless of drawing size, and there is no per-primitive layout.
/// </para>
/// <para>
/// The control holds the parsed scene, so resizing re-runs only the transform and the command
/// build — the file is never re-read.
/// </para>
/// </remarks>
public sealed class SceneView : FrameworkElement
{
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(
        nameof(Scene),
        typeof(Scene),
        typeof(SceneView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSceneChanged));

    /// <summary>
    /// The scale actually applied, as a percentage. Written by the control after each render
    /// and intended to be bound <c>OneWayToSource</c> so a view model can display the zoom.
    /// </summary>
    public static readonly DependencyProperty ZoomPercentageProperty = DependencyProperty.Register(
        nameof(ZoomPercentage),
        typeof(double),
        typeof(SceneView),
        new FrameworkPropertyMetadata(100.0));

    private readonly DrawCommandPainter _painter = new();
    private readonly SceneRenderer _renderer;

    /// <summary>Commands for the current scene and size; rebuilt only when one of them changes.</summary>
    private IReadOnlyList<DrawCommand> _commands = [];
    private bool _commandsAreStale = true;

    public SceneView()
    {
        // The control is created by XAML, so it builds the standard renderer itself. A new
        // primitive is registered in PrimitiveRendererRegistry.CreateDefault(), which is the
        // one place that lists the built-in primitive set.
        _renderer = new SceneRenderer(PrimitiveRendererRegistry.CreateDefault());
        ClipToBounds = true;
    }

    public Scene? Scene
    {
        get => (Scene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    /// <remarks>
    /// The setter is public only because a <c>OneWayToSource</c> binding requires a writable
    /// target; the value is owned and written by this control after each render.
    /// </remarks>
    public double ZoomPercentage
    {
        get => (double)GetValue(ZoomPercentageProperty);
        set => SetValue(ZoomPercentageProperty, value);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);

        // The drawing is fitted to the control, so a resize invalidates the commands and must
        // explicitly request a repaint — a size change alone does not schedule OnRender.
        _commandsAreStale = true;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (Scene is not { } scene)
        {
            _commands = [];
            return;
        }

        if (_commandsAreStale)
        {
            var rendered = _renderer.Render(scene, new ViewportSize(ActualWidth, ActualHeight));
            _commands = rendered.Commands;
            ZoomPercentage = rendered.Transform.ScalePercentage;
            _commandsAreStale = false;
        }

        _painter.Paint(drawingContext, _commands);
    }

    private static void OnSceneChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((SceneView)sender)._commandsAreStale = true;
    }
}
