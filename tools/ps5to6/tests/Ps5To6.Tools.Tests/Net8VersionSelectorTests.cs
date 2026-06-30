using System.Collections.Generic;
using Ps5To6.Tools.Common;
using Xunit;

public class Net8VersionSelectorTests
{
    [Fact]
    public void Picks_highest_net8_compatible_version()
    {
        var candidates = new List<PackageCandidate>
        {
            new("1.0.0", new[] { "net472" }),
            new("2.0.0", new[] { "netstandard2.0" }),
            new("2.1.0", new[] { "net8.0" }),
            new("3.0.0", new[] { "net472" }), // newer but framework-only -> excluded
        };

        FeedResult r = Net8VersionSelector.Select("Noxum.Example", candidates);

        Assert.True(r.Available);
        Assert.Equal("2.1.0", r.SelectedVersion);
    }

    [Fact]
    public void Reports_unavailable_when_no_compatible_candidate()
    {
        var candidates = new List<PackageCandidate> { new("3.0.0", new[] { "net472" }) };
        FeedResult r = Net8VersionSelector.Select("Old.Only", candidates);
        Assert.False(r.Available);
        Assert.Null(r.SelectedVersion);
    }
}
