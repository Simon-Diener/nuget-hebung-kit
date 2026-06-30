using System;
using System.Collections.Generic;
using System.Linq;

namespace Ps5To6.Tools.Common;

public record PackageCandidate(string Version, IReadOnlyList<string> TargetFrameworks);

public record FeedResult(string PackageId, bool Available, string? SelectedVersion);

public static class Net8VersionSelector
{
    public static FeedResult Select(string packageId, IReadOnlyList<PackageCandidate> candidates)
    {
        PackageCandidate? best = candidates
            .Where(c => c.TargetFrameworks.Any(IsNet8Compatible))
            .OrderByDescending(c => ParseVersion(c.Version))
            .FirstOrDefault();

        return best is null
            ? new FeedResult(packageId, false, null)
            : new FeedResult(packageId, true, best.Version);
    }

    private static bool IsNet8Compatible(string tfm)
    {
        string t = tfm.Trim().ToLowerInvariant();
        if (t.StartsWith("net8.0")) return true;
        if (t is "netstandard2.0" or "netstandard2.1") return true;
        // net5.0/net6.0/net7.0 (with or without -windows) are forward-compatible to net8.
        if (t.StartsWith("net5.0") || t.StartsWith("net6.0") || t.StartsWith("net7.0")) return true;
        return false;
    }

    private static Version ParseVersion(string v)
    {
        // Strip any pre-release suffix for ordering (e.g. "2.1.0-beta" -> 2.1.0).
        string core = new string(v.TakeWhile(ch => char.IsDigit(ch) || ch == '.').ToArray());
        return Version.TryParse(core, out Version? parsed) ? parsed! : new Version(0, 0);
    }
}
