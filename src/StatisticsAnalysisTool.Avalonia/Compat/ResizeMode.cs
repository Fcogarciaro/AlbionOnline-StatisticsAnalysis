// Compatibility shim: System.Windows.ResizeMode. Avalonia only has CanResize, see Window.ResizeMode in Compat/Window.cs.
namespace System.Windows;

public enum ResizeMode
{
    NoResize = 0,
    CanMinimize = 1,
    CanResize = 2,
    CanResizeWithGrip = 3
}
