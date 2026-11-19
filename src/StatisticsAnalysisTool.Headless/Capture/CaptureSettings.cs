using System.Collections.Generic;

// Stands in for the desktop tool's SettingsController, which LibpcapPacketProvider reads for the device selection and
// the packet filter. The provider is linked unchanged; the headless CLI fills these two values from its arguments.
namespace StatisticsAnalysisTool.Common.UserSettings;

public sealed class CaptureSettings
{
    /// <summary>Devices with IsSelected = false are skipped; every other up, non-loopback device is opened.</summary>
    public List<NetworkDeviceSettingsObject> NetworkDevices { get; set; } = [];

    /// <summary>BPF filter; null/empty keeps the provider default (Albion Photon ports 5055, 5056, 5058).</summary>
    public string PacketFilter { get; set; }
}

public static class SettingsController
{
    public static CaptureSettings CurrentSettings { get; } = new();
}
