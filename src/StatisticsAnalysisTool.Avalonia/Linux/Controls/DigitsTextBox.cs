// Avalonia port of Common/Controls/DigitsTextBox.cs: a TextBox that accepts digits only (typed or pasted).
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace StatisticsAnalysisTool.Common.Controls;

public class DigitsTextBox : TextBox
{
    // Derived controls look up their theme by their own type; reuse the TextBox theme (MainStyles.axaml).
    protected override System.Type StyleKeyOverride => typeof(TextBox);

    public DigitsTextBox()
    {
        PastingFromClipboard += OnPasting;
    }

    public static bool IsAllowedText(string text) => !string.IsNullOrEmpty(text) && text.All(char.IsDigit);

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (!IsAllowedText(e.Text))
        {
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);
    }

    private async void OnPasting(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var text = clipboard == null ? null : await clipboard.TryGetTextAsync();
        if (IsAllowedText(text))
        {
            SelectedText = text;
            SelectionStart = SelectionEnd;
        }
    }
}
