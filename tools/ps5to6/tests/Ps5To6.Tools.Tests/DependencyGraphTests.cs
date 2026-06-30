using System.Collections.Generic;
using System.Linq;
using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class DependencyGraphTests
{
    private static ProjectInfo P(string id, string path, params string[] refPaths) =>
        new(id, path, new[] { "net8.0" }, true, "PackageReference", null, true,
            new List<PackageRef>(), refPaths);

    [Fact]
    public void Orders_least_dependencies_first()
    {
        // app -> lib -> core
        ProjectInfo core = P("Core", "/s/Core/Core.csproj");
        ProjectInfo lib = P("Lib", "/s/Lib/Lib.csproj", "/s/Core/Core.csproj");
        ProjectInfo app = P("App", "/s/App/App.csproj", "/s/Lib/Lib.csproj");

        var order = DependencyGraph.BottomUpOrder(new[] { app, lib, core })
            .Select(p => p.Id).ToArray();

        Assert.True(order.ToList().IndexOf("Core") < order.ToList().IndexOf("Lib"));
        Assert.True(order.ToList().IndexOf("Lib") < order.ToList().IndexOf("App"));
    }

    [Fact]
    public void Detects_cycles()
    {
        ProjectInfo a = P("A", "/s/A/A.csproj", "/s/B/B.csproj");
        ProjectInfo b = P("B", "/s/B/B.csproj", "/s/A/A.csproj");
        Assert.Throws<System.InvalidOperationException>(
            () => DependencyGraph.BottomUpOrder(new[] { a, b }));
    }
}
