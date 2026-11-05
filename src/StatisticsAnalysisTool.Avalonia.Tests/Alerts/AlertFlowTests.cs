using System.Reflection;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Alert;
using StatisticsAnalysisTool.Avalonia.Tests.Views;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.NetworkModel;

namespace StatisticsAnalysisTool.Avalonia.Tests.Alerts;

/// <summary>An alert that fires runs the whole chain: sound player process, the alert window, and the alert switches itself off.</summary>
[NonParallelizable]
public class AlertFlowTests
{
    private string _root;
    private string _originalPath;
    private List<FileInformation> _originalSounds;
    private string _originalSelectedSound;
    private double _originalVolume;
    private readonly List<global::Avalonia.Controls.Window> _opened = [];
    private IDisposable _openedSubscription;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("sat-alert").FullName;
        _originalPath = Environment.GetEnvironmentVariable("PATH");
        _originalSounds = SoundController.Sounds;
        _originalSelectedSound = SettingsController.CurrentSettings.SelectedAlertSound;
        _originalVolume = SettingsController.CurrentSettings.AlertSoundVolumePercentage;
        // The headless app has no desktop lifetime (which is what Application.Windows reads), so opened windows are observed directly.
        _opened.Clear();
        _openedSubscription = global::Avalonia.Controls.Window.WindowOpenedEvent.AddClassHandler<global::Avalonia.Controls.Window>((window, _) => _opened.Add(window));
    }

    [TearDown]
    public void TearDown()
    {
        _openedSubscription.Dispose();
        _opened.ForEach(w => w.Close());
        Environment.SetEnvironmentVariable("PATH", _originalPath);
        SoundController.Sounds = _originalSounds;
        SettingsController.CurrentSettings.SelectedAlertSound = _originalSelectedSound;
        SettingsController.CurrentSettings.AlertSoundVolumePercentage = _originalVolume;
        Directory.Delete(_root, true);
    }

    private string InstallFakePlayer()
    {
        var bin = Directory.CreateDirectory(Path.Combine(_root, "bin")).FullName;
        var log = Path.Combine(_root, "played.txt");
        var player = Path.Combine(bin, "pw-play");
        File.WriteAllText(player, $"#!/bin/sh\nprintf '%s\\n' \"$*\" >> '{log}'\n");
        File.SetUnixFileMode(player, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Environment.SetEnvironmentVariable("PATH", bin);
        return log;
    }

    private static (AlertController Controller, Alert.Alert Alert) ActivatePriceAlert(bool playSound)
    {
        var item = new Item { UniqueName = "T4_BAG" };
        var controller = new AlertController(new System.Windows.Data.ListCollectionView(new List<Item> { item }));
        controller.SetPriceAlert(item, true, 100, 60, playSound).Should().Be(AlertActivationResult.Success);
        var alert = (Alert.Alert) typeof(AlertController).GetMethod("GetAlert", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, ["T4_BAG"])!;
        return (controller, alert);
    }

    private static MarketResponse Cheap() => new()
    {
        ItemTypeId = "T4_BAG",
        City = "Caerleon",
        QualityLevel = 1,
        SellPriceMin = 50,
        SellPriceMinDate = DateTime.UtcNow
    };

    private static bool WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(25);
        }

        return condition();
    }

    [AvaloniaTest]
    public void TriggeredAlert_PlaysTheSelectedSound_OpensTheAlertWindow_AndDeactivatesTheAlert()
    {
        using var harness = new ViewHarness();
        var log = InstallFakePlayer();
        var sound = Path.Combine(_root, "alert.wav");
        File.WriteAllBytes(sound, [0]);
        SoundController.Sounds = [new FileInformation("alert.wav", sound)];
        SettingsController.CurrentSettings.SelectedAlertSound = "alert.wav";
        SettingsController.CurrentSettings.AlertSoundVolumePercentage = 100;
        var (controller, alert) = ActivatePriceAlert(playSound: true);

        controller.HandleTriggeredAlert(alert, ItemAlertType.PriceThreshold, Cheap());

        WaitFor(() => File.Exists(log)).Should().BeTrue("the player process was started");
        File.ReadAllText(log).Should().Contain(sound);
        _opened.OfType<StatisticsAnalysisTool.Views.ItemAlertWindow>().Should().ContainSingle();
        controller.IsPriceAlertActive("T4_BAG").Should().BeFalse("a fired alert switches itself off");
    }

    [AvaloniaTest]
    public void TriggeredAlert_WithSoundDisabled_StillOpensTheWindowButStartsNoPlayer()
    {
        using var harness = new ViewHarness();
        var log = InstallFakePlayer();
        var sound = Path.Combine(_root, "alert.wav");
        File.WriteAllBytes(sound, [0]);
        SoundController.Sounds = [new FileInformation("alert.wav", sound)];
        SettingsController.CurrentSettings.SelectedAlertSound = "alert.wav";
        var (controller, alert) = ActivatePriceAlert(playSound: false);

        controller.HandleTriggeredAlert(alert, ItemAlertType.PriceThreshold, Cheap());
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(300);

        File.Exists(log).Should().BeFalse();
        _opened.OfType<StatisticsAnalysisTool.Views.ItemAlertWindow>().Should().NotBeEmpty();
    }
}
