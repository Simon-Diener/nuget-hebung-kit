using System;
using System.IO;
using System.Linq;
using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class ProjectParserTests
{
    private static string Fixture(string rel) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", rel);

    [Fact]
    public void Parses_legacy_packages_config_project()
    {
        ProjectInfo p = ProjectParser.Parse(Fixture("Legacy/Legacy.csproj"));

        Assert.False(p.IsSdkStyle);
        Assert.Equal("packages.config", p.PackageStyle);
        Assert.Contains("v4.7.1", string.Join(",", p.TargetFrameworks));
        Assert.Contains(p.Packages, x => x.Id == "Newtonsoft.Json" && x.Version == "12.0.3");
        Assert.Contains(p.Packages, x => x.Id == "System.Memory" && x.IsMicrosoftOrSystem);
        Assert.False(p.IsDependencyOnly); // has Class1.cs
    }

    [Fact]
    public void Parses_sdk_style_project_with_packagereference()
    {
        ProjectInfo p = ProjectParser.Parse(Fixture("Sdk/Sdk.csproj"));

        Assert.True(p.IsSdkStyle);
        Assert.Equal("PackageReference", p.PackageStyle);
        Assert.Equal(new[] { "net8.0" }, p.TargetFrameworks.ToArray());
        Assert.Contains(p.Packages, x => x.Id == "Serilog" && x.Version == "3.1.1");
        Assert.Single(p.ProjectReferencePaths);
    }

    [Fact]
    public void Flags_dependency_only_project()
    {
        ProjectInfo p = ProjectParser.Parse(Fixture("DepOnly/DepOnly.csproj"));
        Assert.True(p.IsDependencyOnly); // no compilable source, only references
    }
}
