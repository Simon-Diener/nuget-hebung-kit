using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Ps5To6.Tools.Common;

namespace Ps5To6.Tools.FeedProbe;

/// <summary>Queries the feeds configured in the target's nuget.config.</summary>
public sealed class NuGetPackageFeed : IPackageFeed
{
    private readonly SourceRepository _repo;

    public NuGetPackageFeed(string nugetConfigDir)
    {
        ISettings settings = Settings.LoadDefaultSettings(nugetConfigDir);
        var provider = new PackageSourceProvider(settings);
        PackageSource source = provider.LoadPackageSources().First(s => s.IsEnabled);
        _repo = Repository.Factory.GetCoreV3(source);
    }

    public async Task<IReadOnlyList<PackageCandidate>> GetCandidatesAsync(string packageId, CancellationToken ct)
    {
        PackageMetadataResource res = await _repo.GetResourceAsync<PackageMetadataResource>(ct);
        using var cache = new SourceCacheContext();
        IEnumerable<IPackageSearchMetadata> all = await res.GetMetadataAsync(
            packageId, includePrerelease: false, includeUnlisted: false, cache, NullLogger.Instance, ct);

        return all.Select(m => new PackageCandidate(
            m.Identity.Version.ToNormalizedString(),
            m.DependencySets.Select(d => d.TargetFramework.GetShortFolderName()).Distinct().ToList()
                as IReadOnlyList<string>)).ToList();
    }
}
