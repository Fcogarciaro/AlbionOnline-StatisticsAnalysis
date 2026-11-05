// Linux replacement for Common/SoundController.cs. The WPF version plays WAV files through
// System.Windows.Media.MediaPlayer. Sound file discovery is identical; playback is a stub.
// TODO(T17): play the WAV through a Linux audio backend (PipeWire/PulseAudio/ALSA).
using Serilog;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StatisticsAnalysisTool.Common;

public static class SoundController
{
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

        Log.Debug("Alert sound playback is not implemented on Linux yet: {soundPath}", soundPath);
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
