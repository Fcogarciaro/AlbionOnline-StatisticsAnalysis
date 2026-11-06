using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Validation;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class SettingsSupportTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void PathValidation_RejectsAnEmptyPath(string path)
    {
        PathValidationRule.GetError(path).Should().Be("Path cannot be empty.");
    }

    [Test]
    public void PathValidation_RejectsAMissingFolderAndAcceptsAnExistingOne()
    {
        var folder = Directory.CreateTempSubdirectory("sat-path").FullName;
        try
        {
            PathValidationRule.GetError(Path.Combine(folder, "missing")).Should().Be("Path does not exist.");
            PathValidationRule.GetError(folder).Should().BeNull();
        }
        finally
        {
            Directory.Delete(folder);
        }
    }

    [Test]
    public void OpenFolder_StartInfoPassesTheAbsolutePathAsOneArgument()
    {
        var info = ExternalLink.CreateOpenFolderStartInfo("/tmp/my folder; rm -rf");

        info.FileName.Should().Be("xdg-open");
        info.UseShellExecute.Should().BeFalse();
        info.ArgumentList.Should().Equal("/tmp/my folder; rm -rf");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("relative/dir")]
    [TestCase("--help")]
    public void OpenFolder_RefusesNonAbsolutePaths(string path) => ExternalLink.IsOpenableFolder(path).Should().BeFalse();

    [Test]
    public void DesktopShortcut_ContentHasTheLauncherFieldsButNoAutostartKey()
    {
        var content = DesktopShortcut.BuildContent("\"/opt/sat/StatisticsAnalysisTool\"", "/opt/sat", "/opt/sat/sat-icon.png");

        content.Should().StartWith("[Desktop Entry]\n")
            .And.Contain("Type=Application\n")
            .And.Contain("Exec=\"/opt/sat/StatisticsAnalysisTool\"\n")
            .And.Contain("Path=/opt/sat\n")
            .And.Contain("Icon=/opt/sat/sat-icon.png\n")
            .And.Contain("Categories=Game;\n");
        content.Should().NotContain("Autostart");
    }

    [Test]
    public void DesktopShortcut_UsesThePackagedFileNameAndThemedIconSoThePackageEntryIsReplacedNotDuplicated()
    {
        DesktopShortcut.FileName.Should().Be("statisticsanalysistool.desktop");
        DesktopShortcut.BuildContent("sat", null, null).Should().Contain("Icon=statisticsanalysistool\n");
    }

    [Test]
    public void DesktopShortcut_WritesTheMenuEntryAndAnExecutableCopyOnTheDesktop()
    {
        var root = Directory.CreateTempSubdirectory("sat-shortcut").FullName;
        try
        {
            var applications = Path.Combine(root, "applications");
            var desktop = Directory.CreateDirectory(Path.Combine(root, "Desktop")).FullName;

            var created = DesktopShortcut.TryCreate(applications, desktop, "\"/opt/sat/sat\"", "/opt/sat", null);

            created.Should().BeTrue();
            var menuEntry = Path.Combine(applications, DesktopShortcut.FileName);
            File.ReadAllText(menuEntry).Should().Contain("Exec=\"/opt/sat/sat\"");
            var desktopEntry = Path.Combine(desktop, DesktopShortcut.FileName);
            File.Exists(desktopEntry).Should().BeTrue();
            (File.GetUnixFileMode(desktopEntry) & UnixFileMode.UserExecute).Should().NotBe(UnixFileMode.None);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void DesktopShortcut_WithoutADesktopFolderStillWritesTheMenuEntry()
    {
        var root = Directory.CreateTempSubdirectory("sat-shortcut2").FullName;
        try
        {
            DesktopShortcut.TryCreate(Path.Combine(root, "applications"), Path.Combine(root, "nodesktop"), "x", null, null).Should().BeTrue();

            File.Exists(Path.Combine(root, "applications", DesktopShortcut.FileName)).Should().BeTrue();
            Directory.Exists(Path.Combine(root, "nodesktop")).Should().BeFalse("the desktop folder is never created");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
