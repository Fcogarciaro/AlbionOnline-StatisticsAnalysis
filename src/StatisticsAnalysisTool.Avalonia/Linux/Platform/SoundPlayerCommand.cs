// Chooses and parameterizes the command-line audio player used for alert sounds.
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsAnalysisTool.Linux.Platform;

internal sealed record SoundPlayerCommand(string FileName, IReadOnlyList<string> Arguments)
{
    /// <summary>Players in preference order: PipeWire, PulseAudio, ALSA.</summary>
    public static readonly string[] Preference = ["pw-play", "paplay", "aplay"];

    /// <summary>Clamps to 0..1 like the WPF MediaPlayer path; non-finite input means full volume.</summary>
    public static double NormalizeVolume(double volumePercentage)
        => double.IsFinite(volumePercentage) ? Math.Clamp(volumePercentage, 0, 100) / 100 : 1;

    /// <summary>Available players, best first, with arguments for the file and volume. Empty when muted or none exist.</summary>
    public static IReadOnlyList<SoundPlayerCommand> Resolve(Func<string, bool> isAvailable, string soundPath, double volumePercentage)
    {
        var volume = NormalizeVolume(volumePercentage);
        if (string.IsNullOrWhiteSpace(soundPath) || volume <= 0)
        {
            return [];
        }

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return Preference.Where(isAvailable).Select(name => name switch
        {
            "pw-play" => new SoundPlayerCommand(name, [$"--volume={volume.ToString("0.00", inv)}", "--", soundPath]),
            "paplay" => new SoundPlayerCommand(name, [$"--volume={(int) Math.Round(volume * 65536)}", "--", soundPath]),
            // aplay has no volume control; the file plays at the mixer level.
            _ => new SoundPlayerCommand(name, ["-q", "--", soundPath])
        }).ToList();
    }
}
