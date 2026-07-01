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
            // A net8-native build outranks a merely forward-compatible (net6/net7/
            // netstandard) build regardless of version: the PS5→PS6 target is the
            // net8-native package set (often shipped as RCs), not the PS5-era build
            // that only happens to install into a net8 project.
            .OrderByDescending(c => c.TargetFrameworks.Any(IsNet8Native))
            .ThenByDescending(c => ParseCore(c.Version))
            .ThenByDescending(c => IsStable(c.Version)) // at equal tier+core, stable beats prerelease/RC
            .FirstOrDefault();

        return best is null
            ? new FeedResult(packageId, false, null)
            : new FeedResult(packageId, true, best.Version);
    }

    private static bool IsNet8Native(string tfm) =>
        tfm.Trim().ToLowerInvariant().StartsWith("net8.0");

    private static bool IsNet8Compatible(string tfm)
    {
        string t = tfm.Trim().ToLowerInvariant();
        if (t.StartsWith("net8.0")) return true;
        if (t is "netstandard2.0" or "netstandard2.1") return true;
        // net5.0/net6.0/net7.0 (with or without -windows) are forward-compatible to net8.
        if (t.StartsWith("net5.0") || t.StartsWith("net6.0") || t.StartsWith("net7.0")) return true;
        return false;
    }

    private static bool IsStable(string v) => !v.Contains('-');

    private static Version ParseCore(string v)
    {
        // Numeric core for ordering (e.g. "6.1.0-rc.1" -> 6.1.0). Prerelease vs
        // stable at the same core is broken by the IsStable tie-break above.
        string core = new string(v.TakeWhile(ch => char.IsDigit(ch) || ch == '.').ToArray());
        return Version.TryParse(core, out Version? parsed) ? parsed! : new Version(0, 0);
    }
}
