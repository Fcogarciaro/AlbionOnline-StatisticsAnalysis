// Linux replacement for Ookii.Dialogs.Wpf.VistaOpenFileDialog (Windows-only). The linked logging code needs
// Filter, Title, Multiselect, InitialDirectory, ShowDialog() and FileNames; backed by Avalonia's StorageProvider.
using Avalonia.Platform.Storage;
using System;
using System.Linq;

namespace Ookii.Dialogs.Wpf;

public class VistaOpenFileDialog
{
    public string Filter { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool Multiselect { get; set; }

    public string InitialDirectory { get; set; } = string.Empty;

    public string[] FileNames { get; private set; } = [];

    /// <summary>True when at least one file was picked; false when cancelled or no window exists.</summary>
    public bool? ShowDialog()
    {
        FileNames = [];
        var filters = Microsoft.Win32.FilePickerFilter.Parse(Filter);
        var request = new StatisticsAnalysisTool.Linux.Platform.PickerRequest
        {
            Kind = StatisticsAnalysisTool.Linux.Platform.PickerKind.OpenFile,
            Title = Title,
            InitialPath = InitialDirectory,
            Multiple = Multiselect,
            Filters = filters
        };

        FileNames = System.Windows.PickerHost.Pick(request, async provider =>
        {
            var options = new FilePickerOpenOptions
            {
                Title = string.IsNullOrWhiteSpace(Title) ? null : Title,
                AllowMultiple = Multiselect,
                FileTypeFilter = filters.Select(e => new FilePickerFileType(e.Name) { Patterns = e.Patterns.ToList() }).ToList(),
                SuggestedStartLocation = System.Windows.PickerHost.StartLocation(provider, InitialDirectory)
            };

            var files = await provider.OpenFilePickerAsync(options);
            return files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToList();
        }).ToArray();
        return FileNames.Length > 0;
    }
}
