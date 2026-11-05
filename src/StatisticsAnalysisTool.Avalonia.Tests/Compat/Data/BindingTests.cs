using System.Globalization;
using System.Windows.Data;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat.Data;

public class BindingTests
{
    private sealed class Doubler : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int) value * 2;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    [Test]
    public void DoNothing_IsSingletonSentinel()
    {
        Binding.DoNothing.Should().BeSameAs(Binding.DoNothing);
        Binding.DoNothing.Should().NotBeNull();
    }

    [Test]
    public void ValueConverter_IsDirectlyAnAvaloniaConverter()
    {
        global::Avalonia.Data.Converters.IValueConverter avalonia = new Doubler();

        avalonia.Convert(21, typeof(int), null, CultureInfo.InvariantCulture).Should().Be(42);
        avalonia.ConvertBack(1, typeof(int), null, CultureInfo.InvariantCulture)
            .Should().BeSameAs(Binding.DoNothing);
    }

    private sealed class Joiner : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => string.Join("|", values);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    [Test]
    public void MultiValueConverter_AdapterPassesValuesAsArray()
    {
        var avalonia = new Joiner().AsAvalonia();

        avalonia.Convert(new List<object> { 1, "b" }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("1|b");
    }
}
