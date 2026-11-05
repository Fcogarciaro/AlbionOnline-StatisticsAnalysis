// Stand-in for a tab content view that is not ported yet (the header, navigation and visibility already work).
// Each use is replaced by the real UserControl when its port task lands; PORTING.md lists them.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace StatisticsAnalysisTool.UserControls;

public sealed class PortingPlaceholderControl : UserControl
{
    public static readonly StyledProperty<string> ViewNameProperty =
        AvaloniaProperty.Register<PortingPlaceholderControl, string>(nameof(ViewName));

    public PortingPlaceholderControl()
    {
        Content = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };
        UpdateText();
    }

    public string ViewName
    {
        get => GetValue(ViewNameProperty);
        set => SetValue(ViewNameProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewNameProperty)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        ((TextBlock) Content).Text = $"Not yet ported: {ViewName}";
    }
}
