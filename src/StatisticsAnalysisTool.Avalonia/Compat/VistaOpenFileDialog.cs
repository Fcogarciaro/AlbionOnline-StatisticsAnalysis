// Linux replacement for Ookii.Dialogs.Wpf.VistaOpenFileDialog (Windows-only). The linked logging code only needs
// Filter, Title, Multiselect, ShowDialog() and FileNames.
// TODO(T03): implement with Avalonia's IStorageProvider.OpenFilePickerAsync (blocking via a nested dispatcher frame).
using System;

namespace Ookii.Dialogs.Wpf;

public class VistaOpenFileDialog
{
    public string Filter { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool Multiselect { get; set; }

    public string InitialDirectory { get; set; } = string.Empty;

    public string[] FileNames { get; private set; } = [];

    /// <summary>Currently always returns false (cancelled) until the Avalonia picker is wired in.</summary>
    public bool? ShowDialog()
    {
        FileNames = [];
        return false;
    }
}
