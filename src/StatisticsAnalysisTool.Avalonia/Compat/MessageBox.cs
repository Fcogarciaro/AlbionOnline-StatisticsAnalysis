// Compatibility shim: System.Windows.MessageBox, Microsoft.Win32.SaveFileDialog and Ookii's folder dialog as
// used by linked ViewModels. Each call blocks the caller (nested dispatcher frame) over an Avalonia modal
// window or the platform StorageProvider, so WPF's synchronous call sites keep working unchanged.
using Avalonia.Platform.Storage;
using System.Windows;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace System.Windows
{
    public static class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
            => Show(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption)
            => Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
            => Show(messageBoxText, caption, button, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            Log.Information("MessageBox [{Icon}] {Caption}: {Text}", icon, caption, messageBoxText);

            if (Avalonia.Application.Current?.ApplicationLifetime == null)
            {
                return MessageBoxLayout.CloseResult(button);
            }

            return BlockingUi.Run(() => System.Threading.Tasks.Task.FromResult(ShowOnUiThread(messageBoxText, caption, button, icon)));
        }

        private static MessageBoxResult ShowOnUiThread(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var dialog = new MessageBoxDialog(text, caption, button, icon);
            dialog.ShowDialogBlocking(BlockingUi.FindOwner());
            return dialog.Result;
        }
    }
}

namespace System.Windows
{
    internal static class PickerHost
    {
        public static IStorageProvider GetStorageProvider()
        {
            var owner = BlockingUi.FindOwner();
            if (owner?.StorageProvider == null)
            {
                Log.Warning("No visible window available to host a file dialog");
            }

            return owner?.StorageProvider;
        }

        public static IStorageFolder StartLocation(IStorageProvider provider, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Directory.Exists(directory))
            {
                return null;
            }

            return BlockingUi.Run(() => provider.TryGetFolderFromPathAsync(directory));
        }
    }
}

namespace Microsoft.Win32
{
    public class SaveFileDialog
    {
        public string FileName { get; set; } = string.Empty;

        public string DefaultExt { get; set; } = string.Empty;

        public string Filter { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string InitialDirectory { get; set; } = string.Empty;

        /// <summary>True when the user picked a file (FileName holds the full path); false when cancelled or no window exists.</summary>
        public bool? ShowDialog()
        {
            var provider = PickerHost.GetStorageProvider();
            if (provider == null)
            {
                return false;
            }

            var entries = FilePickerFilter.Parse(Filter);
            var options = new FilePickerSaveOptions
            {
                Title = string.IsNullOrWhiteSpace(Title) ? null : Title,
                SuggestedFileName = System.IO.Path.GetFileName(FileName),
                DefaultExtension = DefaultExt?.TrimStart('.'),
                ShowOverwritePrompt = true,
                FileTypeChoices = entries.Select(e => new FilePickerFileType(e.Name) { Patterns = e.Patterns.ToList() }).ToList(),
                SuggestedStartLocation = PickerHost.StartLocation(provider, InitialDirectory)
            };

            var file = BlockingUi.Run(() => provider.SaveFilePickerAsync(options));
            var path = file?.TryGetLocalPath();
            if (path == null)
            {
                return false;
            }

            FileName = FilePickerFilter.EnsureExtension(path, DefaultExt);
            return true;
        }
    }
}

namespace Ookii.Dialogs.Wpf
{
    public class VistaFolderBrowserDialog
    {
        public string Description { get; set; } = string.Empty;

        public bool UseDescriptionForTitle { get; set; }

        public string SelectedPath { get; set; } = string.Empty;

        public bool ShowNewFolderButton { get; set; }

        public System.Environment.SpecialFolder RootFolder { get; set; }

        public bool Multiselect { get; set; }

        /// <summary>True when a folder was picked (SelectedPath holds it); false when cancelled or no window exists.</summary>
        public bool? ShowDialog()
        {
            var provider = PickerHost.GetStorageProvider();
            if (provider == null)
            {
                return false;
            }

            var start = PickerHost.StartLocation(provider, SelectedPath)
                        ?? PickerHost.StartLocation(provider, System.Environment.GetFolderPath(RootFolder));
            var options = new FolderPickerOpenOptions
            {
                Title = UseDescriptionForTitle && !string.IsNullOrWhiteSpace(Description) ? Description : null,
                AllowMultiple = Multiselect,
                SuggestedStartLocation = start
            };

            var folders = BlockingUi.Run(() => provider.OpenFolderPickerAsync(options));
            var path = folders.Select(f => f.TryGetLocalPath()).FirstOrDefault(p => p != null);
            if (path == null)
            {
                return false;
            }

            SelectedPath = path;
            return true;
        }
    }
}
