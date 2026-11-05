// Compatibility shim: System.Windows.Clipboard (text only) over Avalonia's clipboard of the main window.
using Avalonia.Controls;
using Avalonia.Input.Platform;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace System.Windows;

public static class Clipboard
{
    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        AvaloniaDispatcher.UIThread.Post(() =>
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                _ = clipboard.SetTextAsync(text);
            }
        });
    }

    public static void SetDataObject(object data) => SetDataObject(data, false);

    public static void SetDataObject(object data, bool copy)
    {
        if (data is string text)
        {
            SetText(text);
        }
    }

    private static IClipboard GetClipboard() => TopLevel.GetTopLevel(Application.Current.MainWindow)?.Clipboard;
}
