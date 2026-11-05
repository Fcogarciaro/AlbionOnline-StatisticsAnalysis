// Compatibility shim: mirrors System.Windows.Visibility so linked WPF logic/ViewModels compile unchanged.
namespace System.Windows;

public enum Visibility : byte
{
    Visible = 0,
    Hidden = 1,
    Collapsed = 2
}
