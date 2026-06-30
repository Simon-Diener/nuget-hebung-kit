using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Ps5To6.Tools.Common;

namespace Ps5To6.Tools.FeedProbe;

/// <summary>
/// Queries every enabled feed in the target's nuget.config. Uses NuGet's default
/// credential service so authenticated feeds (e.g. Azure DevOps Artifacts via the
/// Azure Artifacts Credential Provider / MSAL Integrated Windows Auth) work the
/// same way `dotnet restore` makes them work — a raw NuGet.Protocol call does not
/// load credential-provider plugins on its own.
/// </summary>
public sealed class NuGetPackageFeed : IPackageFeed
{
    private readonly IReadOnlyList<SourceRepository> _repos;

    public NuGetPackageFeed(string nugetConfigDir)
    {
        EnsureCredentialService();
        ISettings settings = Settings.LoadDefaultSettings(nugetConfigDir);
        var provider = new PackageSourceProvider(settings);
        _repos = provider.LoadPackageSources()
            .Where(s => s.IsEnabled)
            .Select(s => Repository.Factory.GetCoreV3(s))
            .ToList();
        if (_repos.Count == 0)
            throw new InvalidOperationException($"No enabled NuGet sources resolved from {nugetConfigDir}.");
    }

    public async Task<IReadOnlyList<PackageCandidate>> GetCandidatesAsync(string packageId, CancellationToken ct)
    {
        using var cache = new SourceCacheContext();
        var candidates = new List<PackageCandidate>();
        foreach (SourceRepository repo in _repos)
        {
            PackageMetadataResource res = await repo.GetResourceAsync<PackageMetadataResource>(ct);
            IEnumerable<IPackageSearchMetadata> all = await res.GetMetadataAsync(
                packageId, includePrerelease: false, includeUnlisted: false, cache, NullLogger.Instance, ct);
            candidates.AddRange(all.Select(m => new PackageCandidate(
                m.Identity.Version.ToNormalizedString(),
                m.DependencySets.Select(d => d.TargetFramework.GetShortFolderName()).Distinct().ToList())));
        }
        return candidates;
    }

    private static int _credInit;

    private static void EnsureCredentialService()
    {
        // Set up once per process: wires in the installed credential-provider
        // plugins (Azure Artifacts, etc.). nonInteractive=false lets MSAL
        // Integrated Windows Auth resolve silently and allows a fallback prompt.
        if (Interlocked.Exchange(ref _credInit, 1) == 0)
            DefaultCredentialServiceUtility.SetupDefaultCredentialService(NullLogger.Instance, nonInteractive: false);
    }
}
