// Pure rules behind TextBoxHelper.OnlyNumeric (ported 1:1 from the WPF TextBoxHelper, which kept the same checks
// in three copies: typed text, pasted text and DefaultValue). Culture handling is unchanged: the current culture's
// decimal separator is used, the other one of "," and "." is accepted and normalized.
using System;
using System.Globalization;
using static StatisticsAnalysisTool.Common.TextBoxHelper;

namespace StatisticsAnalysisTool.Common;

internal static class NumericInputRules
{
    public static bool IsAllowed(string text, NumericFormat format, EvenOddConstraint constraint)
    {
        text ??= string.Empty;
        double number;
        switch (format)
        {
            case NumericFormat.Double:
                if (!IsValidDoubleInput(text, out number))
                {
                    return false;
                }

                break;
            case NumericFormat.Int:
                if (!int.TryParse(text, out var integer))
                {
                    return false;
                }

                number = integer;
                break;
            case NumericFormat.Uint:
                if (!uint.TryParse(text, out var unsigned))
                {
                    return false;
                }

                number = unsigned;
                break;
            case NumericFormat.Natural:
                if (!uint.TryParse(text, out var natural) || natural == 0)
                {
                    return false;
                }

                number = natural;
                break;
            default:
                return false;
        }

        return constraint switch
        {
            EvenOddConstraint.OnlyEven => number % 2 == 0,
            EvenOddConstraint.OnlyOdd => number % 2 != 0,
            _ => true
        };
    }

    /// <summary>The text after typing <paramref name="typed"/> over the selection (<paramref name="selectionStart"/>, <paramref name="selectionLength"/>).</summary>
    public static string Insert(string text, int selectionStart, int selectionLength, string typed)
    {
        text ??= string.Empty;
        var start = Math.Clamp(selectionStart, 0, text.Length);
        var length = Math.Clamp(selectionLength, 0, text.Length - start);
        return text.Remove(start, length).Insert(start, typed ?? string.Empty);
    }

    /// <summary>The text itself when valid, otherwise the default value.</summary>
    public static string Coerce(string text, NumericFormat format, EvenOddConstraint constraint, string defaultValue)
    {
        return IsAllowed(text, format, constraint) ? text : defaultValue;
    }

    private static bool IsValidDoubleInput(string text, out double number)
    {
        if (TryParseDoubleInput(text, out number))
        {
            return true;
        }

        if (IsPendingDecimalInput(text))
        {
            number = 0d;
            return true;
        }

        return false;
    }

    private static bool IsPendingDecimalInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var decimalSeparator in GetDecimalSeparators())
        {
            if (!text.EndsWith(decimalSeparator, StringComparison.Ordinal))
            {
                continue;
            }

            var numberPart = text[..^decimalSeparator.Length];
            if (string.IsNullOrWhiteSpace(numberPart) || TryParseDoubleInput(numberPart, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] GetDecimalSeparators()
    {
        var cultureDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        return cultureDecimalSeparator switch
        {
            "," => [",", "."],
            "." => [".", ","],
            _ => [cultureDecimalSeparator, ",", "."]
        };
    }

    private static bool TryParseDoubleInput(string text, out double number)
    {
        const NumberStyles numberStyles = NumberStyles.AllowLeadingSign
                                         | NumberStyles.AllowDecimalPoint
                                         | NumberStyles.AllowLeadingWhite
                                         | NumberStyles.AllowTrailingWhite;
        if (double.TryParse(text, numberStyles, CultureInfo.CurrentCulture, out number))
        {
            return true;
        }

        return double.TryParse(NormalizeDecimalSeparator(text), numberStyles, CultureInfo.CurrentCulture, out number);
    }

    private static string NormalizeDecimalSeparator(string text)
    {
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var alternateDecimalSeparator = decimalSeparator == "," ? "." : ",";
        if (text.Contains(decimalSeparator, StringComparison.Ordinal)
            || !text.Contains(alternateDecimalSeparator, StringComparison.Ordinal))
        {
            return text;
        }

        return text.Replace(alternateDecimalSeparator, decimalSeparator);
    }
}
