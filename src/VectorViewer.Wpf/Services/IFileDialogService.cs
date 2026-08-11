using Microsoft.Win32;

namespace VectorViewer.Wpf.Services;

/// <summary>Asks the user for a file to open. Abstracted so the view model stays testable.</summary>
public interface IFileDialogService
{
    /// <summary>Returns the chosen path, or <c>null</c> when the user cancels.</summary>
    string? PickFileToOpen(string filter);
}

/// <inheritdoc />
public sealed class FileDialogService : IFileDialogService
{
    public string? PickFileToOpen(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
