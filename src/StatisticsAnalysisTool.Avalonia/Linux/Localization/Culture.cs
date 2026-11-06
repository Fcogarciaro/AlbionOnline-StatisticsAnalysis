// Linux replacement for Localization/Culture.cs. WPF overrides FrameworkElement.LanguageProperty (xml:lang) so
// bindings format with the selected culture. Avalonia formats with CultureInfo.CurrentCulture, so setting the
// thread/default cultures is sufficient and there is no per-window language to update.
using StatisticsAnalysisTool.Common.UserSettings;
using System.Globalization;

namespace StatisticsAnalysisTool.Localization;

public class Culture
{
    public static void SetCulture(CultureInfo cultureInfo)
    {
        if (cultureInfo == null)
        {
            return;
        }

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        SettingsController.CurrentSettings.CurrentCultureIetfLanguageTag = cultureInfo.IetfLanguageTag;
    }

    public static CultureInfo GetCultureByIetfLanguageTag(string ietfLanguageTag)
    {
        if (!string.IsNullOrEmpty(ietfLanguageTag))
        {
            return new CultureInfo(ietfLanguageTag);
        }

        return null;
    }
}
