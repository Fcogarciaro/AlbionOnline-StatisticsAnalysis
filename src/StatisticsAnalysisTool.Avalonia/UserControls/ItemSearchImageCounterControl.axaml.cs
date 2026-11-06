using Avalonia;
using Avalonia.Controls;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/ItemSearchImageCounterControl.xaml(.cs): same two properties. As upstream, the labels bind to the inherited
/// data context (the main view model), not to the properties.
/// </summary>
public partial class ItemSearchImageCounterControl : UserControl
{
    public static readonly StyledProperty<string> LocalImageCounterProperty =
        AvaloniaProperty.Register<ItemSearchImageCounterControl, string>(nameof(LocalImageCounter));

    public static readonly StyledProperty<string> ItemCounterStringProperty =
        AvaloniaProperty.Register<ItemSearchImageCounterControl, string>(nameof(ItemCounterString));

    public ItemSearchImageCounterControl()
    {
        InitializeComponent();
    }

    public string LocalImageCounter
    {
        get => GetValue(LocalImageCounterProperty);
        set => SetValue(LocalImageCounterProperty, value);
    }

    public string ItemCounterString
    {
        get => GetValue(ItemCounterStringProperty);
        set => SetValue(ItemCounterStringProperty, value);
    }
}
