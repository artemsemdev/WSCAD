using System.Windows.Input;

namespace VectorViewer.Wpf.ViewModels;

/// <summary>
/// The minimal <see cref="ICommand"/> this application needs.
/// </summary>
/// <remarks>
/// Roughly thirty lines instead of an MVVM framework dependency: the app has two commands,
/// and a framework would add configuration without adding clarity.
/// </remarks>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}
