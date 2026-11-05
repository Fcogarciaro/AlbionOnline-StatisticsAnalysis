// Pure logic of the Linux updater: GitHub release payload parsing, tag version parsing and update selection.
// No I/O here so it is unit tested; AutoUpdateController wires it to HTTP and the UpdateWindow.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StatisticsAnalysisTool.Linux.Updater;

public sealed record ReleaseAsset(string Name, string DownloadUrl);

public sealed record ReleaseInfo(
    string TagName,
    string Name,
    string Body,
    string HtmlUrl,
    DateTimeOffset? PublishedAt,
    bool IsDraft,
    bool IsPrerelease,
    IReadOnlyList<ReleaseAsset> Assets);

public sealed record UpdateCandidate(ReleaseInfo Release, Version Version, ReleaseAsset Tarball, ReleaseAsset Checksum);

public static partial class ReleaseVersion
{
    public const string TagPrefix = "linux-v";

    [GeneratedRegex(@"^\d+(\.\d+){0,3}")]
    private static partial Regex NumericPrefix();

    /// <summary>Reads <c>linux-v1.2.3</c> (any case, optional <c>-rc1</c> or <c>+build</c> suffix). Other tags are not Linux releases.</summary>
    public static bool TryParse(string tag, out Version version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var match = NumericPrefix().Match(tag[TagPrefix.Length..]);
        if (!match.Success)
        {
            return false;
        }

        var text = match.Value.Contains('.') ? match.Value : match.Value + ".0";
        return Version.TryParse(text, out version);
    }

    /// <summary>Compares versions with missing components counted as zero (<c>1.2</c> equals <c>1.2.0.0</c>).</summary>
    public static int Compare(Version left, Version right)
    {
        static int[] Parts(Version v) => [v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0)];

        var l = Parts(left);
        var r = Parts(right);
        for (var i = 0; i < l.Length; i++)
        {
            if (l[i] != r[i])
            {
                return l[i].CompareTo(r[i]);
            }
        }

        return 0;
    }
}

public static class UpdateSelector
{
    public const string TarballName = "StatisticsAnalysisTool-linux-x64.tar.gz";
    public const string ChecksumName = TarballName + ".sha256";

    /// <summary>The newest published Linux release that is newer than <paramref name="current"/> and ships the tarball, or null.</summary>
    public static UpdateCandidate Select(IEnumerable<ReleaseInfo> releases, Version current, bool includePrereleases)
    {
        ArgumentNullException.ThrowIfNull(current);

        return (releases ?? [])
            .Where(r => r is { IsDraft: false } && (includePrereleases || !r.IsPrerelease))
            .Select(r => (Release: r, Parsed: ReleaseVersion.TryParse(r.TagName, out var v), Version: v))
            .Where(x => x.Parsed && ReleaseVersion.Compare(x.Version, current) > 0)
            .Select(x => new UpdateCandidate(
                x.Release,
                x.Version,
                x.Release.Assets?.FirstOrDefault(a => string.Equals(a.Name, TarballName, StringComparison.Ordinal)),
                x.Release.Assets?.FirstOrDefault(a => string.Equals(a.Name, ChecksumName, StringComparison.Ordinal))))
            .Where(c => c.Tarball != null)
            .OrderByDescending(c => c.Version, Comparer<Version>.Create(ReleaseVersion.Compare))
            .FirstOrDefault();
    }
}

public static class GitHubReleaseParser
{
    /// <summary>Parses the <c>GET /repos/{owner}/{repo}/releases</c> array. Anything else (error object, garbage) yields an empty list.</summary>
    public static IReadOnlyList<ReleaseInfo> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object)
                .Select(ParseRelease)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ReleaseInfo ParseRelease(JsonElement e)
    {
        var assets = e.TryGetProperty("assets", out var assetArray) && assetArray.ValueKind == JsonValueKind.Array
            ? assetArray.EnumerateArray().Where(a => a.ValueKind == JsonValueKind.Object)
                .Select(a => new ReleaseAsset(Text(a, "name"), Text(a, "browser_download_url"))).ToList()
            : [];

        DateTimeOffset? published = DateTimeOffset.TryParse(Text(e, "published_at"), System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var date) ? date : null;

        return new ReleaseInfo(Text(e, "tag_name"), Text(e, "name"), Text(e, "body"), Text(e, "html_url"), published,
            Flag(e, "draft"), Flag(e, "prerelease"), assets);
    }

    private static string Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Flag(JsonElement e, string name) => e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
