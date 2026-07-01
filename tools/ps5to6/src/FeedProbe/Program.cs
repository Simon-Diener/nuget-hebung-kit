using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ps5To6.Tools.Common;
using Ps5To6.Tools.FeedProbe;

// Usage: ps5to6-feed-probe <nugetConfigDir> <packageListFile> <outputJson> [--stable-only]
// packageListFile: one package id per line.
// Prerelease/RC versions are INCLUDED by default (PS6 packages often ship as RCs);
// pass --stable-only to consider stable releases exclusively.
string[] positional = args.Where(a => !a.StartsWith("--")).ToArray();
bool includePrerelease = !args.Contains("--stable-only");
if (positional.Length != 3)
{
    Console.Error.WriteLine("Usage: ps5to6-feed-probe <nugetConfigDir> <packageListFile> <outputJson> [--stable-only]");
    return 2;
}

string[] ids = File.ReadAllLines(positional[1])
    .Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToArray();

IPackageFeed feed = new NuGetPackageFeed(positional[0]);
var results = new List<FeedResult>();
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

foreach (string id in ids)
{
    IReadOnlyList<PackageCandidate> candidates = await feed.GetCandidatesAsync(id, includePrerelease, cts.Token);
    FeedResult r = Net8VersionSelector.Select(id, candidates);
    results.Add(r);
    Console.WriteLine($"{id}: {(r.Available ? r.SelectedVersion : "NO net8 version")}");
}

File.WriteAllText(positional[2],
    System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
return 0;
