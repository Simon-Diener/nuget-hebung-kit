using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ps5To6.Tools.Common;

public interface IPackageFeed
{
    Task<IReadOnlyList<PackageCandidate>> GetCandidatesAsync(string packageId, bool includePrerelease, CancellationToken ct);
}
