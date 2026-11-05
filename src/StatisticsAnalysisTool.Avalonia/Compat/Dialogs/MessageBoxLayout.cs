// Pure layout rules for the MessageBox shim: which buttons a MessageBoxButton shows, what each returns, and
// what closing the window without pressing a button means (WPF semantics).
using System.Collections.Generic;

namespace System.Windows;

internal readonly record struct MessageBoxChoice(string Label, MessageBoxResult Result);

internal static class MessageBoxLayout
{
    public static IReadOnlyList<MessageBoxChoice> Choices(MessageBoxButton button) => button switch
    {
        MessageBoxButton.OKCancel => [new("OK", MessageBoxResult.OK), new("Cancel", MessageBoxResult.Cancel)],
        MessageBoxButton.YesNo => [new("Yes", MessageBoxResult.Yes), new("No", MessageBoxResult.No)],
        MessageBoxButton.YesNoCancel =>
            [new("Yes", MessageBoxResult.Yes), new("No", MessageBoxResult.No), new("Cancel", MessageBoxResult.Cancel)],
        _ => [new("OK", MessageBoxResult.OK)]
    };

    /// <summary>Result when the dialog is dismissed with Escape or the close button.</summary>
    public static MessageBoxResult CloseResult(MessageBoxButton button) => button switch
    {
        MessageBoxButton.OKCancel or MessageBoxButton.YesNoCancel => MessageBoxResult.Cancel,
        MessageBoxButton.YesNo => MessageBoxResult.No,
        _ => MessageBoxResult.OK
    };

    /// <summary>Glyph drawn inside the icon badge, or null for <see cref="MessageBoxImage.None"/>.</summary>
    public static string IconGlyph(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => "\u2715",
        MessageBoxImage.Warning => "!",
        MessageBoxImage.Information => "i",
        MessageBoxImage.Question => "?",
        _ => null
    };
}
