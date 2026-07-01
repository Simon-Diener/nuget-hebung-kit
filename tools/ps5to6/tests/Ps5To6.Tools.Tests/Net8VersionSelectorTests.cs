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

    [Fact]
    public void Selects_release_candidate_when_only_prerelease_exists()
    {
        // PS6 packages often ship only as RCs on the feed.
        var candidates = new List<PackageCandidate> { new("6.0.0-rc.2", new[] { "net8.0" }) };
        FeedResult r = Net8VersionSelector.Select("Noxum.OnlyRc", candidates);
        Assert.True(r.Available);
        Assert.Equal("6.0.0-rc.2", r.SelectedVersion);
    }

    [Fact]
    public void Prefers_stable_over_prerelease_at_equal_core_version()
    {
        var candidates = new List<PackageCandidate>
        {
            new("6.0.0-rc.2", new[] { "net8.0" }),
            new("6.0.0", new[] { "net8.0" }),
        };
        FeedResult r = Net8VersionSelector.Select("Noxum.Both", candidates);
        Assert.Equal("6.0.0", r.SelectedVersion);
    }

    [Fact]
    public void Higher_prerelease_beats_lower_stable()
    {
        var candidates = new List<PackageCandidate>
        {
            new("6.0.0", new[] { "net8.0" }),
            new("6.1.0-rc.1", new[] { "net8.0" }),
        };
        FeedResult r = Net8VersionSelector.Select("Noxum.Newer", candidates);
        Assert.Equal("6.1.0-rc.1", r.SelectedVersion);
    }

    [Fact]
    public void Prefers_net8_native_rc_over_forward_compatible_stable_at_equal_core()
    {
        // The exact PS5→PS6 trap: a stable net6 build (forward-compatible) next to
        // the net8-native RC. PS6 wants the net8-native package, not the PS5-era
        // net6 build that merely happens to install into a net8 project.
        var candidates = new List<PackageCandidate>
        {
            new("2.1.0", new[] { "net6.0" }),      // PS5-era stable, forward-compatible only
            new("2.1.0-rc.1", new[] { "net8.0" }), // the PS6 net8-native build
        };
        FeedResult r = Net8VersionSelector.Select("Noxum.Ps6", candidates);
        Assert.Equal("2.1.0-rc.1", r.SelectedVersion);
    }

    [Fact]
    public void Net8_native_beats_higher_core_forward_compatible()
    {
        // Native net8 wins even when a forward-compatible build has a higher core
        // version — the migration target is the net8-native package set.
        var candidates = new List<PackageCandidate>
        {
            new("3.0.0", new[] { "net6.0" }), // higher core, forward-compatible only
            new("2.0.0", new[] { "net8.0" }), // lower core, but net8-native
        };
        FeedResult r = Net8VersionSelector.Select("Noxum.Ps6b", candidates);
        Assert.Equal("2.0.0", r.SelectedVersion);
    }
}
