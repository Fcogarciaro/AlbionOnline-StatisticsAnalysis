using System.Windows;
using System.Windows.Media;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class ApplicationResourcesTests
{
    private static void Register()
    {
        var resources = global::Avalonia.Application.Current!.Resources;
        resources["Color.Test"] = global::Avalonia.Media.Color.FromArgb(1, 2, 3, 4);
        resources["SolidColorBrush.Test"] = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromRgb(9, 8, 7));
        resources["Plain"] = "text";
    }

    [AvaloniaTest]
    public void Indexer_ConvertsAvaloniaColorToShimColor()
    {
        Register();

        var color = (Color) Application.Current.Resources["Color.Test"]!;

        color.Should().Be(Color.FromArgb(1, 2, 3, 4));
    }

    [AvaloniaTest]
    public void Indexer_ConvertsAvaloniaSolidBrushToShimSolidColorBrush()
    {
        Register();

        var brush = Application.Current.Resources["SolidColorBrush.Test"].Should().BeOfType<SolidColorBrush>().Subject;

        brush.Color.Should().Be(Color.FromRgb(9, 8, 7));
        (Application.Current.Resources["SolidColorBrush.Test"] is Brush).Should().BeTrue();
    }

    [AvaloniaTest]
    public void Indexer_ReturnsPlainValuesAndNullForMissing()
    {
        Register();

        Application.Current.Resources["Plain"].Should().Be("text");
        Application.Current.Resources["Missing.Key"].Should().BeNull();
    }

    [AvaloniaTest]
    public void Contains_ReflectsPresence()
    {
        Register();

        Application.Current.Resources.Contains("Plain").Should().BeTrue();
        Application.Current.Resources.Contains("Missing.Key").Should().BeFalse();
    }

    [AvaloniaTest]
    public void TryFindResource_ReturnsConvertedValueOrNull()
    {
        Register();

        Application.Current.TryFindResource("SolidColorBrush.Test").Should().BeOfType<SolidColorBrush>();
        Application.Current.TryFindResource("Missing.Key").Should().BeNull();
    }
}
