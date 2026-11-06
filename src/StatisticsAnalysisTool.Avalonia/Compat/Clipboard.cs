// Compatibility shim: System.Windows.Clipboard (text only) over Avalonia's clipboard of the main window.
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>Test seam: the window whose clipboard is used.</summary>
    internal static Func<TopLevel> ResolveOverride { get; set; }

    /// <summary>
    /// The main window owns the clipboard when it exists (even hidden to the tray); alert and dialog windows can copy before
    /// it exists or after it closed, so any visible window serves as a fallback.
    /// </summary>
    internal static TopLevel PickWindow(TopLevel mainWindow, IEnumerable<Avalonia.Controls.Window> windows) =>
        mainWindow ?? windows.FirstOrDefault(w => w.IsVisible);

    private static IClipboard GetClipboard() =>
        (ResolveOverride?.Invoke() ?? PickWindow(Application.Current.MainWindow, Application.Current.Windows))?.Clipboard;
}
