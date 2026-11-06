using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.Converters;
using AvaloniaBinding = Avalonia.Data.Binding;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

[NonParallelizable]
public class ConverterAndBindingTests
{
    private CultureInfo _previous;

    [SetUp]
    public void SetUp()
    {
        _previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    [TearDown]
    public void TearDown() => CultureInfo.CurrentCulture = _previous;

    // Binds a source value through a converter to TextBlock.Text exactly like a ported view would.
    private static string Bound(object source, global::Avalonia.Data.Converters.IValueConverter converter, object parameter = null)
    {
        var text = new TextBlock { DataContext = source };
        text.Bind(TextBlock.TextProperty, new AvaloniaBinding(".") { Converter = converter, ConverterParameter = parameter });
        return text.Text;
    }

    [Test]
    public void UpstreamValueConverters_WorkThroughAvaloniaBindings()
    {
        Bound(0UL, new ZeroToDashConverter()).Should().Be("-");
        Bound(5UL, new ZeroToDashConverter()).Should().Be("5");
        Bound(2.5d, new ZeroToDashConverter()).Should().Be("2.5");
        Bound(1234567d, new ValuePerHourConverter()).Should().Be("1,234,567/h");
        Bound(double.NaN, new ValuePerHourConverter()).Should().Be("0/h");
        Bound(1234.4d, new ValueAvgConverter()).Should().Be("1,234 Avg.");
        Bound(3.14159d, new RoundingConverter(), "2").Should().Be("3.14");
        Bound(3661, new SecondsToTimeFormatConverter()).Should().Be("01:01:01");
        Bound(-1, new IntToStringOrQuestionMarkConverter()).Should().Be("?");
        Bound(7, new IntToStringOrQuestionMarkConverter()).Should().Be("7");
        Bound(-1, new EnumToIntOrQuestionMarkConverter()).Should().Be("?");
        Bound(4000d, new DurationSecondsToCompactStringConverter()).Should().Be("1h 6m");
        Bound(1500000, new ShortNumberConverter()).Should().NotBeNullOrEmpty();
    }

    [TestCase(System.Windows.Visibility.Visible, true)]
    [TestCase(System.Windows.Visibility.Hidden, false)]
    [TestCase(System.Windows.Visibility.Collapsed, false)]
    public void VisibilityToBool_MapsTheShimEnumToIsVisible(System.Windows.Visibility visibility, bool expected)
    {
        VisibilityToBoolConverter.Instance.Convert(visibility, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Test]
    public void VisibilityToBool_PassesBooleansThroughAndTreatsOtherValuesAsHidden()
    {
        VisibilityToBoolConverter.Instance.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
        VisibilityToBoolConverter.Instance.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        VisibilityToBoolConverter.Instance.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        VisibilityToBoolConverter.Instance.Convert("x", typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Test]
    public void VisibilityToBool_InvertedInstanceFlipsTheResultAndConvertBackRestoresTheEnum()
    {
        VisibilityToBoolConverter.Inverted.Convert(System.Windows.Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        VisibilityToBoolConverter.Inverted.Convert(System.Windows.Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
        VisibilityToBoolConverter.Instance.ConvertBack(true, typeof(System.Windows.Visibility), null, CultureInfo.InvariantCulture).Should().Be(System.Windows.Visibility.Visible);
        VisibilityToBoolConverter.Instance.ConvertBack(false, typeof(System.Windows.Visibility), null, CultureInfo.InvariantCulture).Should().Be(System.Windows.Visibility.Collapsed);
    }

    [AvaloniaTest]
    public void IsVisibleBinding_FollowsAVisibilityProperty()
    {
        var source = new VisibilitySource { Visibility = System.Windows.Visibility.Collapsed };
        var panel = new Panel { DataContext = source };
        panel.Bind(Control.IsVisibleProperty, new AvaloniaBinding(nameof(VisibilitySource.Visibility)) { Converter = VisibilityToBoolConverter.Instance });

        panel.IsVisible.Should().BeFalse();
        source.Visibility = System.Windows.Visibility.Visible;
        panel.IsVisible.Should().BeTrue();
    }

    [Test]
    public void CultureAwareBinding_UsesTheCurrentCultureForTheConversion()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        var binding = (AvaloniaBinding) new CultureAwareBinding("Amount") { StringFormat = "{0:N0}", FallbackValue = 0 }.ProvideValue(null);

        binding.Path.Should().Be("Amount");
        binding.ConverterCulture.Should().Be(new CultureInfo("de-DE"));
        binding.StringFormat.Should().Be("{0:N0}");
        binding.FallbackValue.Should().Be(0);
    }

    [AvaloniaTest]
    public void CultureAwareBinding_FormatsNumbersWithTheCultureAtBindingTime()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        var text = new TextBlock { DataContext = new { Amount = 1234567.0 } };

        text.Bind(TextBlock.TextProperty, (AvaloniaBinding) new CultureAwareBinding("Amount") { StringFormat = "{0:N0}" }.ProvideValue(null));

        text.Text.Should().Be("1.234.567");
    }

    [AvaloniaTest]
    public void CultureAwareBinding_WorksAsAMarkupExtensionInXaml()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        var block = (TextBlock) AvaloniaRuntimeXamlLoader.Load("""
            <TextBlock xmlns="https://github.com/avaloniaui" xmlns:common="clr-namespace:StatisticsAnalysisTool.Common"
                       Text="{common:CultureAwareBinding Amount, StringFormat='{}{0:N0}', FallbackValue=0}" />
            """);
        block.DataContext = new { Amount = 9876543.0 };

        block.Text.Should().Be("9.876.543");
    }

    private sealed class VisibilitySource : System.ComponentModel.INotifyPropertyChanged
    {
        private System.Windows.Visibility _visibility;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public System.Windows.Visibility Visibility
        {
            get => _visibility;
            set
            {
                _visibility = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Visibility)));
            }
        }
    }
}
