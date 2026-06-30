using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ps5To6.Tools.Common;
using Ps5To6.Tools.FeedProbe;

// Usage: ps5to6-feed-probe <nugetConfigDir> <packageListFile> <outputJson>
// packageListFile: one package id per line.
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ps5to6-feed-probe <nugetConfigDir> <packageListFile> <outputJson>");
    return 2;
}

string[] ids = File.ReadAllLines(args[1])
    .Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToArray();

IPackageFeed feed = new NuGetPackageFeed(args[0]);
var results = new List<FeedResult>();
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

foreach (string id in ids)
{
    IReadOnlyList<PackageCandidate> candidates = await feed.GetCandidatesAsync(id, cts.Token);
    FeedResult r = Net8VersionSelector.Select(id, candidates);
    results.Add(r);
    Console.WriteLine($"{id}: {(r.Available ? r.SelectedVersion : "NO net8 version")}");
}

File.WriteAllText(args[2],
    System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
return 0;
