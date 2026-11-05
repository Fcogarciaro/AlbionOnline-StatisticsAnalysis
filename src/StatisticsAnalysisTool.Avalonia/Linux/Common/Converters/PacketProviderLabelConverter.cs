// The settings view model names the packet providers "Sockets (must be run as admin)" and "Npcap" (hard-coded in
// SettingsWindowViewModel.InitPacketProvider). On Linux both run libpcap: Npcap (value 2) is shown as "libpcap" and Sockets
// (value 1, possibly from a Windows settings file) as "Sockets (mapped to libpcap)", with the same texts as the first start guide.
using Avalonia.Data.Converters;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class PacketProviderLabelConverter : IValueConverter
{
    public const string LibpcapLabel = "libpcap";
    public const string SocketsLabel = "Sockets (mapped to libpcap)";

    public static readonly PacketProviderLabelConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SettingsWindowViewModel.SettingDataInformation information)
        {
            return null;
        }

        return information.Value switch
        {
            2 => LibpcapLabel,
            1 => SocketsLabel,
            _ => information.Name
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
