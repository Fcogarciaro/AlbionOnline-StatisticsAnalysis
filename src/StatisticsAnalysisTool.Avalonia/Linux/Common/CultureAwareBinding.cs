// Avalonia port of Common/CultureAwareBinding.cs. WPF derived from Binding and pinned ConverterCulture to the current
// culture (XAML bindings otherwise format with en-US). Avalonia bindings are not markup extensions, so this is a
// markup extension that returns the equivalent Binding: `{common:CultureAwareBinding Path, StringFormat=..., FallbackValue=0}`.
// IsAsync does not exist in Avalonia and must be dropped from ported XAML.
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace StatisticsAnalysisTool.Common;

public class CultureAwareBinding : MarkupExtension
{
    public CultureAwareBinding()
    {
    }

    public CultureAwareBinding(string path)
    {
        Path = path;
    }

    public string Path { get; set; }

    public string StringFormat { get; set; }

    public object FallbackValue { get; set; } = AvaloniaProperty_UnsetValue;

    public BindingMode Mode { get; set; } = BindingMode.Default;

    public IValueConverter Converter { get; set; }

    public object ConverterParameter { get; set; }

    private static readonly object AvaloniaProperty_UnsetValue = global::Avalonia.AvaloniaProperty.UnsetValue;

    public override Binding ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(Path ?? string.Empty, Mode)
        {
            StringFormat = StringFormat,
            Converter = Converter,
            ConverterParameter = ConverterParameter,
            ConverterCulture = CultureInfo.CurrentCulture
        };

        if (!ReferenceEquals(FallbackValue, AvaloniaProperty_UnsetValue))
        {
            binding.FallbackValue = FallbackValue;
        }

        return binding;
    }
}
