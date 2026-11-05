// Compatibility shim: System.Windows.MessageBox, Microsoft.Win32.SaveFileDialog and Ookii's folder dialog as
// used by linked ViewModels. Each call blocks the caller (nested dispatcher frame) over an Avalonia modal
// window or the platform StorageProvider, so WPF's synchronous call sites keep working unchanged.
using Avalonia.Platform.Storage;
using System.Windows;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StatisticsAnalysisTool.Linux.Platform;

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
            // The window list is UI-thread state: look the owner up there, whatever thread the caller is on.
            var provider = BlockingUi.Run(() => System.Threading.Tasks.Task.FromResult(BlockingUi.FindOwner()?.StorageProvider));
            if (provider == null)
            {
                Log.Warning("No visible window available to host a file dialog");
            }

            return provider;
        }

        private static readonly ExternalFilePicker ExternalPicker = ExternalFilePicker.CreateDefault();

        /// <summary>
        /// Runs a picker through the platform provider, or the external zenity/kdialog picker when the process is
        /// non-dumpable (setcap) or the provider throws. Returns the picked paths, empty when cancelled.
        /// </summary>
        public static IReadOnlyList<string> Pick(
            PickerRequest request, Func<IStorageProvider, Task<IReadOnlyList<string>>> viaProvider)
        {
            var provider = GetStorageProvider();
            if (provider == null)
            {
                return [];
            }

            var outcome = BlockingUi.Run(() => PickerRouter.PickAsync(
                request, () => viaProvider(provider), ExternalPicker, ProcessDumpability.IsCurrentProcessNonDumpable()));
            if (outcome.ToolMissing)
            {
                MessageBox.Show(PickerRouter.MissingToolMessage, string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return outcome.Paths;
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
            var entries = FilePickerFilter.Parse(Filter);
            var request = new PickerRequest
            {
                Kind = PickerKind.SaveFile,
                Title = Title,
                InitialPath = InitialDirectory,
                SuggestedName = System.IO.Path.GetFileName(FileName),
                Filters = entries
            };

            var paths = PickerHost.Pick(request, async provider =>
            {
                var options = new FilePickerSaveOptions
                {
                    Title = string.IsNullOrWhiteSpace(Title) ? null : Title,
                    SuggestedFileName = System.IO.Path.GetFileName(FileName),
                    DefaultExtension = DefaultExt?.TrimStart('.'),
                    ShowOverwritePrompt = true,
                    FileTypeChoices = entries.Select(e => new FilePickerFileType(e.Name) { Patterns = e.Patterns.ToList() }).ToList(),
                    SuggestedStartLocation = PickerHost.StartLocation(provider, InitialDirectory)
                };

                var file = await provider.SaveFilePickerAsync(options);
                var local = file?.TryGetLocalPath();
                return local == null ? [] : new[] { local };
            });

            var path = paths.FirstOrDefault();
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
            var request = new PickerRequest
            {
                Kind = PickerKind.Folder,
                Title = UseDescriptionForTitle ? Description : null,
                InitialPath = System.IO.Directory.Exists(SelectedPath) ? SelectedPath : System.Environment.GetFolderPath(RootFolder),
                Multiple = Multiselect
            };

            var paths = PickerHost.Pick(request, async provider =>
            {
                var start = PickerHost.StartLocation(provider, SelectedPath)
                            ?? PickerHost.StartLocation(provider, System.Environment.GetFolderPath(RootFolder));
                var options = new FolderPickerOpenOptions
                {
                    Title = UseDescriptionForTitle && !string.IsNullOrWhiteSpace(Description) ? Description : null,
                    AllowMultiple = Multiselect,
                    SuggestedStartLocation = start
                };

                var folders = await provider.OpenFolderPickerAsync(options);
                return folders.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToList();
            });

            var path = paths.FirstOrDefault();
            if (path == null)
            {
                return false;
            }

            SelectedPath = path;
            return true;
        }
    }
}
