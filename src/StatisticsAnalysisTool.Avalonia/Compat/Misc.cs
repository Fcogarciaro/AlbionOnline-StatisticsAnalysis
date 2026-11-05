// Compatibility shim: small System.Windows types referenced by linked non-view code.
using System.Collections.Concurrent;

namespace System.Windows
{
    public enum WindowState
    {
        Normal = 0,
        Minimized = 1,
        Maximized = 2
    }

    public enum MessageBoxButton { OK = 0, OKCancel = 1, YesNoCancel = 3, YesNo = 4 }

    public enum MessageBoxImage { None = 0, Error = 16, Question = 32, Warning = 48, Information = 64 }

    public enum MessageBoxResult { None = 0, OK = 1, Cancel = 2, Yes = 6, No = 7 }
}

namespace System.Windows.Markup
{
    public sealed class XmlLanguage
    {
        private static readonly ConcurrentDictionary<string, XmlLanguage> Cache = new(StringComparer.OrdinalIgnoreCase);

        private XmlLanguage(string ietfLanguageTag)
        {
            IetfLanguageTag = ietfLanguageTag;
        }

        public string IetfLanguageTag { get; }

        public static XmlLanguage GetLanguage(string ietfLanguageTag)
        {
            ArgumentNullException.ThrowIfNull(ietfLanguageTag);
            return Cache.GetOrAdd(ietfLanguageTag, tag => new XmlLanguage(tag.ToLowerInvariant()));
        }
    }
}
