// Linux replacement for the MSBuild-generated AutoUpdateSecurityBuildSettings.g.cs of the WPF project.
// Automatic updates are disabled on Linux (see AutoUpdateController), so signature settings stay empty.
namespace StatisticsAnalysisTool.Updater;

internal static class AutoUpdateSecurityBuildSettings
{
    public const bool AreBackgroundUpdatesEnabled = false;
    public const bool IsSignatureVerificationRequired = false;
    public const string AuthenticodePublisherThumbprint = "";
    public const string Ed25519PublicKey = "";
}
