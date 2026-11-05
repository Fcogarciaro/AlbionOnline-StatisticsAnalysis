// Linux replacement for Common/SoundController.cs. The WPF version plays WAV files through
// System.Windows.Media.MediaPlayer. Sound file discovery is identical; playback shells out to the first available
// of pw-play, paplay, aplay (see SoundPlayerCommand), non-blocking.
using Serilog;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Models;
using System;
using StatisticsAnalysisTool.Linux.Platform;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StatisticsAnalysisTool.Common;

public static class SoundController
{
    private static bool _hasWarnedAboutMissingPlayer;

    public static List<FileInformation> Sounds { get; set; } = new();

    public static void InitializeSoundFilesFromDirectory()
    {
        if (Sounds?.Count > 0)
        {
            return;
        }

        var soundFilePath = AppDataPaths.SoundDirectory;

        if (!Directory.Exists(soundFilePath))
        {
            return;
        }

        var files = DirectoryController.GetFiles(soundFilePath, "*.wav");

        if (files == null)
        {
            return;
        }

        Sounds ??= new List<FileInformation>();

        foreach (var file in files)
        {
            Sounds.Add(new FileInformation(Path.GetFileNameWithoutExtension(file), file));
        }
    }

    public static void PlayAlertSound(string soundPath, double volumePercentage = 100)
    {
        if (string.IsNullOrWhiteSpace(soundPath))
        {
            return;
        }

        var commands = SoundPlayerCommand.Resolve(ExecutableLocator.IsAvailable, soundPath, volumePercentage);
        if (commands.Count == 0)
        {
            if (SoundPlayerCommand.NormalizeVolume(volumePercentage) > 0 && !_hasWarnedAboutMissingPlayer)
            {
                _hasWarnedAboutMissingPlayer = true;
                Log.Warning("No audio player found ({players}); alert sounds are disabled", string.Join(", ", SoundPlayerCommand.Preference));
            }

            return;
        }

        foreach (var command in commands)
        {
            if (TryStart(command, soundPath))
            {
                return;
            }
        }
    }

    // Fire and forget: the player process exits on its own when the clip ends.
    private static bool TryStart(SoundPlayerCommand command, string soundPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo(command.FileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in command.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.Exited += (_, _) => process.Dispose();
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return true;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            Log.Warning(e, "Alert sound player {player} failed for {soundPath}", command.FileName, soundPath);
            return false;
        }
    }

    public static string GetCurrentSoundPath(string selectedAlertSound)
    {
        try
        {
            var currentSound = Sounds.FirstOrDefault(s => s.FileName == selectedAlertSound);
            return currentSound?.FilePath ?? string.Empty;
        }
        catch (Exception e) when (e is ArgumentException)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            return string.Empty;
        }
    }
}
