// Avalonia window behind System.Windows.MessageBox: message text, optional icon badge and a row of buttons.
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace System.Windows;

internal sealed class MessageBoxDialog : Window
{
    private readonly MessageBoxButton _button;

    public MessageBoxDialog(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        _button = button;
        Result = MessageBoxLayout.CloseResult(button);
        Title = caption ?? string.Empty;
        CanResize = false;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 320;
        MaxWidth = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        Button first = null;
        foreach (var choice in MessageBoxLayout.Choices(button))
        {
            var captured = choice;
            var control = new Button { Content = captured.Label, MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, Name = $"Button{captured.Result}" };
            control.Click += (_, _) => Finish(captured.Result);
            buttons.Children.Add(control);
            first ??= control;
        }

        var body = new DockPanel { LastChildFill = true };
        var glyph = MessageBoxLayout.IconGlyph(icon);
        if (glyph != null)
        {
            var badge = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = BadgeBrush(icon),
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock { Text = glyph, Foreground = Brushes.White, FontWeight = FontWeight.Bold, FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            DockPanel.SetDock(badge, Dock.Left);
            body.Children.Add(badge);
        }

        body.Children.Add(new TextBlock { Text = text ?? string.Empty, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });

        var root = new DockPanel { Margin = new Thickness(20) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        buttons.Margin = new Thickness(0, 20, 0, 0);
        root.Children.Add(buttons);
        root.Children.Add(body);
        Content = root;

        Opened += (_, _) => first?.Focus();
    }

    public MessageBoxResult Result { get; private set; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            e.Handled = true;
            Finish(MessageBoxLayout.CloseResult(_button));
        }
    }

    private void Finish(MessageBoxResult result)
    {
        Result = result;
        Close();
    }

    private static IBrush BadgeBrush(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => Brushes.Firebrick,
        MessageBoxImage.Warning => Brushes.DarkOrange,
        MessageBoxImage.Question => Brushes.SteelBlue,
        _ => Brushes.RoyalBlue
    };
}
