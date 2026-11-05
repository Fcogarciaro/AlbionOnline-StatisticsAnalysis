// Compatibility shim: WPF registers the "pack" URI scheme; .NET on Linux does not, so
// new Uri("pack://application:,,,/x.png") throws. Registering a generic parser restores parsing.
using System.Runtime.CompilerServices;

namespace System.Windows.Compat;

public static class PackUri
{
    [ModuleInitializer]
    public static void EnsureRegistered()
    {
        if (UriParser.IsKnownScheme("pack"))
        {
            return;
        }

        UriParser.Register(new PackUriParser(), "pack", -1);
    }

    // The default parser accepts the "application:,,," authority as part of the path.
    private sealed class PackUriParser : UriParser
    {
    }
}
