// Compatibility shim: System.Windows.Window on top of Avalonia's Window. Ported views derive from this type so
// linked ViewModels/controllers can use the WPF surface they were written against (Dispatcher, Left/Top,
// DialogResult, synchronous ShowDialog()).
using System.Threading.Tasks;
using System.Windows.Threading;
using Avalonia;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;
using DispatcherFrame = Avalonia.Threading.DispatcherFrame;
using AvaloniaWindow = Avalonia.Controls.Window;

namespace System.Windows;

public class Window : AvaloniaWindow
{
    private bool? _dialogResult;
    private bool _resultAssigned;

    public Dispatcher Dispatcher => Dispatcher.CurrentDispatcher;

    public double Left
    {
        get => Position.X;
        set => Position = new PixelPoint((int) value, Position.Y);
    }

    public double Top
    {
        get => Position.Y;
        set => Position = new PixelPoint(Position.X, (int) value);
    }

    /// <summary>Like WPF, assigning a dialog result closes a window shown with ShowDialog().</summary>
    public bool? DialogResult
    {
        get => _dialogResult;
        set
        {
            _dialogResult = value;
            _resultAssigned = true;
            Close(value);
        }
    }

    /// <summary>Synchronous modal dialog over the application's main window (WPF ShowDialog()).</summary>
    public bool? ShowDialog() => ShowDialogBlocking(Application.Current.MainWindow);

    /// <summary>
    /// Shows the window modally and blocks the caller (running a nested dispatcher frame) until it closes,
    /// returning the dialog result like WPF. Without an owner the window is shown non-modally.
    /// </summary>
    public bool? ShowDialogBlocking(AvaloniaWindow owner)
    {
        if (!AvaloniaDispatcher.UIThread.CheckAccess())
        {
            return AvaloniaDispatcher.UIThread.Invoke(() => ShowDialogBlocking(owner));
        }

        var frame = new DispatcherFrame();
        Closed += (_, _) => frame.Continue = false;

        if (owner != null)
        {
            _ = ShowDialog<bool?>(owner);
        }
        else
        {
            Show();
        }

        AvaloniaDispatcher.UIThread.PushFrame(frame);
        return _resultAssigned ? _dialogResult : false;
    }
}
