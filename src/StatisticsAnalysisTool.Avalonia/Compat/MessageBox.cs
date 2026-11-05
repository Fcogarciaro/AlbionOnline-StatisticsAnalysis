// Compatibility shim: System.Windows.MessageBox and the Microsoft.Win32 / Ookii file dialogs referenced by
// linked ViewModels. Real Avalonia dialogs arrive with the platform layer.
// TODO(T03): implement message boxes and file/folder pickers with Avalonia (nested dispatcher frame).
using Serilog;

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
            return button switch
            {
                MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel => MessageBoxResult.No,
                MessageBoxButton.OKCancel => MessageBoxResult.Cancel,
                _ => MessageBoxResult.OK
            };
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

        /// <summary>Always cancelled until the Avalonia save picker is wired in.</summary>
        public bool? ShowDialog() => false;
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

        /// <summary>Always cancelled until the Avalonia folder picker is wired in.</summary>
        public bool? ShowDialog() => false;
    }
}
