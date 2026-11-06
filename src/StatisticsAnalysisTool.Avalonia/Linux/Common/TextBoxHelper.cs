// Avalonia port of Common/TextBoxHelper.cs: the attached properties OnlyNumeric, DefaultValue and EvenOddConstraint
// keep their names and values, so ported XAML stays `common:TextBoxHelper.OnlyNumeric="Double"`.
// Differences: typed text is filtered on the tunnelling TextInput event; paste is intercepted through
// TextBox.PastingFromClipboard and re-applied (replacing the selection) only when the resulting text is valid.
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace StatisticsAnalysisTool.Common;

public sealed class TextBoxHelper
{
    private TextBoxHelper()
    {
    }

    public enum NumericFormat
    {
        Double,
        Int,
        Uint,
        Natural
    }

    public enum EvenOddConstraint
    {
        All,
        OnlyEven,
        OnlyOdd
    }

    public static readonly AttachedProperty<NumericFormat?> OnlyNumericProperty =
        AvaloniaProperty.RegisterAttached<TextBoxHelper, TextBox, NumericFormat?>("OnlyNumeric");

    public static readonly AttachedProperty<string> DefaultValueProperty =
        AvaloniaProperty.RegisterAttached<TextBoxHelper, TextBox, string>("DefaultValue");

    public static readonly AttachedProperty<EvenOddConstraint> EvenOddConstraintProperty =
        AvaloniaProperty.RegisterAttached<TextBoxHelper, TextBox, EvenOddConstraint>("EvenOddConstraint", EvenOddConstraint.All);

    static TextBoxHelper()
    {
        OnlyNumericProperty.Changed.AddClassHandler<TextBox>(OnOnlyNumericChanged);
        DefaultValueProperty.Changed.AddClassHandler<TextBox>(OnDefaultValueChanged);
    }

    public static void SetOnlyNumeric(TextBox element, NumericFormat? value) => element.SetValue(OnlyNumericProperty, value);

    public static NumericFormat? GetOnlyNumeric(TextBox element) => element.GetValue(OnlyNumericProperty);

    public static void SetDefaultValue(TextBox element, string value) => element.SetValue(DefaultValueProperty, value);

    public static string GetDefaultValue(TextBox element) => element.GetValue(DefaultValueProperty);

    public static void SetEvenOddConstraint(TextBox element, EvenOddConstraint value) => element.SetValue(EvenOddConstraintProperty, value);

    public static EvenOddConstraint GetEvenOddConstraint(TextBox element) => element.GetValue(EvenOddConstraintProperty);

    private static void OnOnlyNumericChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        textBox.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
        textBox.RemoveHandler(TextBox.PastingFromClipboardEvent, OnPasting);
        if (e.NewValue is NumericFormat)
        {
            textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
            textBox.AddHandler(TextBox.PastingFromClipboardEvent, OnPasting, RoutingStrategies.Bubble);
        }
    }

    private static void OnDefaultValueChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        textBox.TextChanged -= OnTextChanged;
        if (e.NewValue is string)
        {
            textBox.TextChanged += OnTextChanged;
        }
    }

    private static void OnTextInput(object sender, TextInputEventArgs e)
    {
        var textBox = (TextBox) sender;
        if (GetOnlyNumeric(textBox) is not { } format || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var newText = NumericInputRules.Insert(textBox.Text, textBox.SelectionStart, textBox.SelectionEnd - textBox.SelectionStart, e.Text);
        e.Handled = !NumericInputRules.IsAllowed(newText, format, GetEvenOddConstraint(textBox));
    }

    private static async void OnPasting(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox) sender;
        if (GetOnlyNumeric(textBox) is not { } format)
        {
            return;
        }

        e.Handled = true;
        var clipboard = TopLevel.GetTopLevel(textBox)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        var clipboardText = await clipboard.TryGetTextAsync();
        if (string.IsNullOrEmpty(clipboardText))
        {
            return;
        }

        var start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
        var length = Math.Abs(textBox.SelectionEnd - textBox.SelectionStart);
        var newText = NumericInputRules.Insert(textBox.Text, start, length, clipboardText);
        if (NumericInputRules.IsAllowed(newText, format, GetEvenOddConstraint(textBox)))
        {
            textBox.Text = newText;
            textBox.CaretIndex = start + clipboardText.Length;
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox) sender;
        var defaultValue = GetDefaultValue(textBox);
        if (defaultValue == null || GetOnlyNumeric(textBox) is not { } format)
        {
            return;
        }

        var coerced = NumericInputRules.Coerce(textBox.Text, format, GetEvenOddConstraint(textBox), defaultValue);
        if (!string.Equals(coerced, textBox.Text, StringComparison.Ordinal))
        {
            textBox.Text = coerced;
        }
    }
}
