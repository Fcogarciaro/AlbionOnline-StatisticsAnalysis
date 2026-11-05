// Replacement for the WPF <Element.InputBindings><MouseBinding MouseAction="LeftClick|LeftDoubleClick" Command="..."/>:
//   common:MouseCommands.LeftClick="{Binding SomeCommand}" common:MouseCommands.CommandParameter="{Binding Other}"
// LeftClick fires on the left button press (as the WPF MouseBinding does), LeftDoubleClick on the second press.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Windows.Input;

namespace StatisticsAnalysisTool.Common;

public class MouseCommands
{
    public static readonly AttachedProperty<ICommand> LeftClickProperty =
        AvaloniaProperty.RegisterAttached<MouseCommands, Control, ICommand>("LeftClick");

    public static readonly AttachedProperty<ICommand> LeftDoubleClickProperty =
        AvaloniaProperty.RegisterAttached<MouseCommands, Control, ICommand>("LeftDoubleClick");

    public static readonly AttachedProperty<object> CommandParameterProperty =
        AvaloniaProperty.RegisterAttached<MouseCommands, Control, object>("CommandParameter");

    static MouseCommands()
    {
        LeftClickProperty.Changed.AddClassHandler<Control>(CommandChanged);
        LeftDoubleClickProperty.Changed.AddClassHandler<Control>(CommandChanged);
    }

    public static ICommand GetLeftClick(Control control) => control.GetValue(LeftClickProperty);

    public static void SetLeftClick(Control control, ICommand value) => control.SetValue(LeftClickProperty, value);

    public static ICommand GetLeftDoubleClick(Control control) => control.GetValue(LeftDoubleClickProperty);

    public static void SetLeftDoubleClick(Control control, ICommand value) => control.SetValue(LeftDoubleClickProperty, value);

    public static object GetCommandParameter(Control control) => control.GetValue(CommandParameterProperty);

    public static void SetCommandParameter(Control control, object value) => control.SetValue(CommandParameterProperty, value);

    private static void CommandChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.PointerPressed -= PointerPressed;
        if (GetLeftClick(control) != null || GetLeftDoubleClick(control) != null)
        {
            control.PointerPressed += PointerPressed;
        }
    }

    private static void PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var command = e.ClickCount >= 2 ? GetLeftDoubleClick(control) : GetLeftClick(control);
        var parameter = GetCommandParameter(control);
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }
}
