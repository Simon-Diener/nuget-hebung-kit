# PS5→PS6 SFA Tooling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the five C# single-file-app tools (`snapshot`, `uninstall-all`, `feed-probe`, `scaffold-project`, `report`) plus a shared library, all unit-tested against synthetic fixtures, that the PS5→PS6 migration skill drives.

**Architecture:** One .NET solution under `tools/ps5to6/`. A shared `Ps5To6.Tools.Common` library holds all parsing/graph/selection logic (the testable core). Each tool is a thin console app whose `Program.cs` only parses args and calls the library. One xUnit test project exercises the library against committed fixtures. The live-feed query is isolated behind a pure version-selection function so feed logic is testable offline.

**Tech Stack:** .NET 8 (`net8.0`), C#, `System.Xml.Linq` for project parsing, `System.Text.Json` for I/O, `NuGet.Protocol` + `NuGet.Frameworks` for the feed adapter, xUnit for tests.

## Global Constraints

- Target framework for every tool and the library: `net8.0` — exact value `net8.0`.
- Output language: English only (code, comments, identifiers, docs).
- Explicit types over `var` in production C# code.
- Async end-to-end where I/O is involved: `Async` suffix, forward `CancellationToken`, never `.Result`/`.Wait()`.
- Unit tests only; no integration tests against any live feed or shared system. `feed-probe` tests run fully offline.
- No uninvited fallbacks: missing required config/inputs fail loudly (`throw`), not silent defaults.
- Each tool must be publishable as a self-contained single-file exe (`PublishSingleFile=true`).
- Conventional Commits, one logical change per commit. Branch is already `feature/agentic-coding-dogma` (non-protected).
- Tools live under `tools/ps5to6/`; run state the tools read/write lives under the target's `docs/ps5-to-ps6/` (not created by these tools — the skill owns it).

---

### Task 1: Scaffold the tools solution, shared library, and test project

**Files:**
- Create: `tools/ps5to6/Ps5To6.Tools.sln`
- Create: `tools/ps5to6/src/Common/Ps5To6.Tools.Common.csproj`
- Create: `tools/ps5to6/src/Common/Placeholder.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/Ps5To6.Tools.Tests.csproj`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/SmokeTest.cs`
- Create: `tools/ps5to6/.gitignore`

**Interfaces:**
- Consumes: nothing.
- Produces: a buildable solution; `Ps5To6.Tools.Common` namespace; `dotnet test tools/ps5to6/Ps5To6.Tools.sln` runs.

- [ ] **Step 1: Create the Common library project**

`tools/ps5to6/src/Common/Ps5To6.Tools.Common.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>Ps5To6.Tools.Common</RootNamespace>
  </PropertyGroup>
</Project>
```

`tools/ps5to6/src/Common/Placeholder.cs`:
```csharp
namespace Ps5To6.Tools.Common;

/// <summary>Temporary type so the library compiles before real code lands. Deleted in Task 2.</summary>
public static class Placeholder
{
    public static string Marker() => "ps5to6";
}
```

- [ ] **Step 2: Create the test project**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/Ps5To6.Tools.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/tests/Ps5To6.Tools.Tests/SmokeTest.cs`:
```csharp
using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class SmokeTest
{
    [Fact]
    public void Library_is_referenced()
    {
        Assert.Equal("ps5to6", Placeholder.Marker());
    }
}
```

- [ ] **Step 3: Create the solution and add projects**

Run:
```bash
cd tools/ps5to6
dotnet new sln -n Ps5To6.Tools
dotnet sln add src/Common/Ps5To6.Tools.Common.csproj
dotnet sln add tests/Ps5To6.Tools.Tests/Ps5To6.Tools.Tests.csproj
```

`tools/ps5to6/.gitignore`:
```
bin/
obj/
dist/
```

- [ ] **Step 4: Build and test**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: build succeeds; 1 test passes (`Library_is_referenced`).

- [ ] **Step 5: Commit**

```bash
git add tools/ps5to6
git commit -m "chore(tools): scaffold ps5to6 tools solution, common lib, tests"
```

---

### Task 2: Project model + single-project parser

**Files:**
- Create: `tools/ps5to6/src/Common/ProjectInfo.cs`
- Create: `tools/ps5to6/src/Common/ProjectParser.cs`
- Delete: `tools/ps5to6/src/Common/Placeholder.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/ProjectParserTests.cs`
- Create fixtures: `tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Legacy/Legacy.csproj`, `tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Legacy/packages.config`, `tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Sdk/Sdk.csproj`, `tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/DepOnly/DepOnly.csproj`
- Modify: `tools/ps5to6/tests/Ps5To6.Tools.Tests/Ps5To6.Tools.Tests.csproj` (copy fixtures to output)

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `record PackageRef(string Id, string? Version, bool IsMicrosoftOrSystem)`
  - `record ProjectInfo(string Id, string Path, IReadOnlyList<string> TargetFrameworks, bool IsSdkStyle, string PackageStyle, string? OutputType, bool IsDependencyOnly, IReadOnlyList<PackageRef> Packages, IReadOnlyList<string> ProjectReferencePaths)` where `PackageStyle ∈ {"PackageReference","packages.config","none"}`
  - `static class ProjectParser { static ProjectInfo Parse(string csprojPath); }`

- [ ] **Step 1: Write the failing tests**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/ProjectParserTests.cs`:
```csharp
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
```

- [ ] **Step 2: Create the fixtures**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Legacy/Legacy.csproj` (legacy, non-SDK):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Class1.cs" />
  </ItemGroup>
  <ItemGroup>
    <None Include="packages.config" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Legacy/packages.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="12.0.3" targetFramework="net471" />
  <package id="System.Memory" version="4.5.4" targetFramework="net471" />
</packages>
```

`tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/Sdk/Sdk.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog" Version="3.1.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\DepOnly\DepOnly.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/tests/Ps5To6.Tools.Tests/fixtures/DepOnly/DepOnly.csproj` (SDK-style, references only, no source):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog" Version="3.1.1" />
  </ItemGroup>
</Project>
```

Add to `Ps5To6.Tools.Tests.csproj` `<Project>` (so fixtures land next to the test dll):
```xml
  <ItemGroup>
    <None Include="fixtures/**/*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```
Note: for SDK-style test project the implicit-glob would try to compile `fixtures/**/*.cs`; there are none here, but add `<EnableDefaultCompileItems>true</EnableDefaultCompileItems>` is fine — fixtures contain no `.cs`. Keep fixtures free of `.cs` files (the dependency-only check is path-based, see Step 4).

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `ProjectParser` / `ProjectInfo` / `PackageRef` do not exist.

- [ ] **Step 4: Implement the model and parser**

`tools/ps5to6/src/Common/ProjectInfo.cs`:
```csharp
using System.Collections.Generic;

namespace Ps5To6.Tools.Common;

public record PackageRef(string Id, string? Version, bool IsMicrosoftOrSystem);

public record ProjectInfo(
    string Id,
    string Path,
    IReadOnlyList<string> TargetFrameworks,
    bool IsSdkStyle,
    string PackageStyle,
    string? OutputType,
    bool IsDependencyOnly,
    IReadOnlyList<PackageRef> Packages,
    IReadOnlyList<string> ProjectReferencePaths);
```

`tools/ps5to6/src/Common/ProjectParser.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ps5To6.Tools.Common;

public static class ProjectParser
{
    public static ProjectInfo Parse(string csprojPath)
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException($"Project not found: {csprojPath}");

        string fullPath = Path.GetFullPath(csprojPath);
        string dir = Path.GetDirectoryName(fullPath)!;
        string id = Path.GetFileNameWithoutExtension(fullPath);
        XDocument doc = XDocument.Load(fullPath);
        XElement root = doc.Root!;

        bool isSdk = root.Attribute("sdk") is not null
                     || root.Attribute("Sdk") is not null;

        // Element lookups must be namespace-agnostic: legacy projects use the
        // MSBuild xmlns, SDK-style projects use none.
        IEnumerable<XElement> ByLocalName(string name) =>
            root.Descendants().Where(e => e.Name.LocalName == name);

        List<string> tfms = ReadTargetFrameworks(ByLocalName);
        string? outputType = ByLocalName("OutputType").FirstOrDefault()?.Value.Trim();

        (string style, List<PackageRef> packages) = ReadPackages(dir, isSdk, ByLocalName);

        List<string> projectRefs = ByLocalName("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!.Replace('\\', Path.DirectorySeparatorChar))))
            .ToList();

        bool depOnly = IsDependencyOnly(dir, isSdk, ByLocalName);

        return new ProjectInfo(id, fullPath, tfms, isSdk, style, outputType, depOnly, packages, projectRefs);
    }

    private static List<string> ReadTargetFrameworks(Func<string, IEnumerable<XElement>> byLocal)
    {
        // SDK-style: <TargetFramework> or <TargetFrameworks> (semicolon list).
        string? single = byLocal("TargetFramework").FirstOrDefault()?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(single)) return new List<string> { single! };

        string? multi = byLocal("TargetFrameworks").FirstOrDefault()?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(multi))
            return multi!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // Legacy: <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>
        string? legacy = byLocal("TargetFrameworkVersion").FirstOrDefault()?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(legacy)) return new List<string> { legacy! };

        return new List<string>();
    }

    private static (string style, List<PackageRef> packages) ReadPackages(
        string dir, bool isSdk, Func<string, IEnumerable<XElement>> byLocal)
    {
        // packages.config wins if present (legacy projects).
        string packagesConfig = Path.Combine(dir, "packages.config");
        if (File.Exists(packagesConfig))
        {
            List<PackageRef> fromConfig = XDocument.Load(packagesConfig)
                .Descendants()
                .Where(e => e.Name.LocalName == "package")
                .Select(e => Make(e.Attribute("id")?.Value, e.Attribute("version")?.Value))
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList();
            return ("packages.config", fromConfig);
        }

        List<PackageRef> fromRefs = byLocal("PackageReference")
            .Select(e => Make(e.Attribute("Include")?.Value,
                              e.Attribute("Version")?.Value
                              ?? e.Elements().FirstOrDefault(x => x.Name.LocalName == "Version")?.Value))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        return (fromRefs.Count > 0 ? "PackageReference" : "none", fromRefs);
    }

    private static PackageRef? Make(string? id, string? version)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        bool ms = id!.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                  || id.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
                  || id.Equals("NETStandard.Library", StringComparison.OrdinalIgnoreCase);
        return new PackageRef(id, string.IsNullOrWhiteSpace(version) ? null : version, ms);
    }

    private static bool IsDependencyOnly(string dir, bool isSdk, Func<string, IEnumerable<XElement>> byLocal)
    {
        // Explicit <Compile> items (legacy or opted-in SDK) => has source.
        bool explicitCompile = byLocal("Compile")
            .Any(e => (e.Attribute("Include")?.Value ?? "").EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        if (explicitCompile) return false;

        // SDK-style implicit glob: any .cs on disk (excluding obj/bin) => has source.
        if (isSdk)
        {
            bool anyCs = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Any(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                       && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
            return !anyCs;
        }

        // Legacy with no <Compile> items => dependency-only.
        return true;
    }
}
```

Delete `tools/ps5to6/src/Common/Placeholder.cs` and update `SmokeTest.cs` to not reference it:
```csharp
using Xunit;

namespace Ps5To6.Tools.Tests;

public class SmokeTest
{
    [Fact]
    public void Harness_runs() => Assert.True(true);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS (all ProjectParser tests + smoke).

- [ ] **Step 6: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add project model and csproj/packages.config parser"
```

---

### Task 3: Dependency graph + bottom-up topological order

**Files:**
- Create: `tools/ps5to6/src/Common/DependencyGraph.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/DependencyGraphTests.cs`

**Interfaces:**
- Consumes: `ProjectInfo` (Task 2).
- Produces:
  - `static class DependencyGraph`
  - `static IReadOnlyList<ProjectInfo> BottomUpOrder(IReadOnlyList<ProjectInfo> projects)` — least-dependencies-first (a project appears after all projects it references). Throws `InvalidOperationException` on a reference cycle.
  - `static IReadOnlyDictionary<string, IReadOnlyList<string>> Edges(IReadOnlyList<ProjectInfo> projects)` — projectId → referenced projectIds (resolved by path).

- [ ] **Step 1: Write the failing tests**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/DependencyGraphTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `DependencyGraph` does not exist.

- [ ] **Step 3: Implement the graph**

`tools/ps5to6/src/Common/DependencyGraph.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ps5To6.Tools.Common;

public static class DependencyGraph
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Edges(
        IReadOnlyList<ProjectInfo> projects)
    {
        Dictionary<string, string> byPath = projects.ToDictionary(
            p => Norm(p.Path), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var edges = new Dictionary<string, IReadOnlyList<string>>();
        foreach (ProjectInfo p in projects)
        {
            List<string> refs = p.ProjectReferencePaths
                .Select(Norm)
                .Where(byPath.ContainsKey)
                .Select(rp => byPath[rp])
                .ToList();
            edges[p.Id] = refs;
        }
        return edges;
    }

    public static IReadOnlyList<ProjectInfo> BottomUpOrder(IReadOnlyList<ProjectInfo> projects)
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> edges = Edges(projects);
        Dictionary<string, ProjectInfo> byId = projects.ToDictionary(p => p.Id);

        var ordered = new List<ProjectInfo>();
        var state = new Dictionary<string, int>(); // 0=unvisited,1=visiting,2=done

        void Visit(string id)
        {
            state.TryGetValue(id, out int s);
            if (s == 2) return;
            if (s == 1) throw new InvalidOperationException($"Project reference cycle at '{id}'.");
            state[id] = 1;
            foreach (string dep in edges[id]) Visit(dep);
            state[id] = 2;
            ordered.Add(byId[id]);
        }

        foreach (ProjectInfo p in projects) Visit(p.Id);
        return ordered; // dependencies emitted before dependents = bottom-up
    }

    private static string Norm(string path) => Path.GetFullPath(path);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add project-reference graph and bottom-up ordering"
```

---

### Task 4: `snapshot` tool — inventory.json + inventory.md

**Files:**
- Create: `tools/ps5to6/src/Common/SolutionScanner.cs`
- Create: `tools/ps5to6/src/Common/Inventory.cs`
- Create: `tools/ps5to6/src/Snapshot/Snapshot.csproj`
- Create: `tools/ps5to6/src/Snapshot/Program.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/InventoryTests.cs`
- Modify: `tools/ps5to6/Ps5To6.Tools.sln` (add Snapshot project)

**Interfaces:**
- Consumes: `ProjectParser`, `DependencyGraph`.
- Produces:
  - `static class SolutionScanner { static IReadOnlyList<ProjectInfo> ScanDirectory(string rootDir); }` — finds every `*.csproj` under a root.
  - `record InventoryProject(string Id, string Path, IReadOnlyList<string> TargetFrameworks, bool IsSdkStyle, string PackageStyle, string Classification, IReadOnlyList<PackageRef> Packages, IReadOnlyList<string> DependsOn)` where `Classification ∈ {"code","dependency-only"}`.
  - `record InventoryDoc(string GeneratedFromRoot, IReadOnlyList<string> BottomUpOrder, IReadOnlyList<InventoryProject> Projects)`.
  - `static class Inventory { static InventoryDoc Build(string rootDir); static string ToJson(InventoryDoc doc); static string ToMarkdown(InventoryDoc doc); }`

- [ ] **Step 1: Write the failing tests**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/InventoryTests.cs`:
```csharp
using System.IO;
using System.Linq;
using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class InventoryTests
{
    private static string FixturesRoot =>
        Path.Combine(AppContext.BaseDirectory, "fixtures");

    [Fact]
    public void Build_collects_all_projects_classified_and_ordered()
    {
        InventoryDoc doc = Inventory.Build(FixturesRoot);

        Assert.Contains(doc.Projects, p => p.Id == "DepOnly" && p.Classification == "dependency-only");
        Assert.Contains(doc.Projects, p => p.Id == "Legacy" && p.Classification == "code");
        // DepOnly is referenced by Sdk => must come first.
        Assert.True(doc.BottomUpOrder.ToList().IndexOf("DepOnly")
                    < doc.BottomUpOrder.ToList().IndexOf("Sdk"));
    }

    [Fact]
    public void Json_roundtrips_and_markdown_lists_projects()
    {
        InventoryDoc doc = Inventory.Build(FixturesRoot);
        string json = Inventory.ToJson(doc);
        Assert.Contains("\"bottomUpOrder\"", json);

        string md = Inventory.ToMarkdown(doc);
        Assert.Contains("| DepOnly |", md);
        Assert.Contains("dependency-only", md);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `SolutionScanner` / `Inventory` do not exist.

- [ ] **Step 3: Implement scanner and inventory**

`tools/ps5to6/src/Common/SolutionScanner.cs`:
```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ps5To6.Tools.Common;

public static class SolutionScanner
{
    public static IReadOnlyList<ProjectInfo> ScanDirectory(string rootDir)
    {
        if (!Directory.Exists(rootDir))
            throw new DirectoryNotFoundException($"Root not found: {rootDir}");

        return Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .OrderBy(f => f)
            .Select(ProjectParser.Parse)
            .ToList();
    }
}
```

`tools/ps5to6/src/Common/Inventory.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ps5To6.Tools.Common;

public record InventoryProject(
    string Id, string Path, IReadOnlyList<string> TargetFrameworks,
    bool IsSdkStyle, string PackageStyle, string Classification,
    IReadOnlyList<PackageRef> Packages, IReadOnlyList<string> DependsOn);

public record InventoryDoc(
    string GeneratedFromRoot,
    IReadOnlyList<string> BottomUpOrder,
    IReadOnlyList<InventoryProject> Projects);

public static class Inventory
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static InventoryDoc Build(string rootDir)
    {
        IReadOnlyList<ProjectInfo> projects = SolutionScanner.ScanDirectory(rootDir);
        IReadOnlyDictionary<string, IReadOnlyList<string>> edges = DependencyGraph.Edges(projects);
        IReadOnlyList<string> order = DependencyGraph.BottomUpOrder(projects).Select(p => p.Id).ToList();

        List<InventoryProject> invProjects = projects.Select(p => new InventoryProject(
            p.Id, p.Path, p.TargetFrameworks, p.IsSdkStyle, p.PackageStyle,
            p.IsDependencyOnly ? "dependency-only" : "code",
            p.Packages, edges[p.Id])).ToList();

        return new InventoryDoc(rootDir, order, invProjects);
    }

    public static string ToJson(InventoryDoc doc) => JsonSerializer.Serialize(doc, JsonOpts);

    public static string ToMarkdown(InventoryDoc doc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PS5→PS6 — Inventory (IST state)");
        sb.AppendLine();
        sb.AppendLine($"Root: `{doc.GeneratedFromRoot}`");
        sb.AppendLine();
        sb.AppendLine("## Bottom-up order");
        sb.AppendLine();
        sb.AppendLine(string.Join(" → ", doc.BottomUpOrder));
        sb.AppendLine();
        sb.AppendLine("## Projects");
        sb.AppendLine();
        sb.AppendLine("| Project | Class | TFM(s) | Style | Packages | Depends on |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (InventoryProject p in doc.Projects)
        {
            string pkgs = string.Join(", ", p.Packages.Select(x => $"{x.Id} {x.Version}".Trim()));
            sb.AppendLine($"| {p.Id} | {p.Classification} | {string.Join(";", p.TargetFrameworks)} " +
                          $"| {p.PackageStyle} | {pkgs} | {string.Join(", ", p.DependsOn)} |");
        }
        return sb.ToString();
    }
}
```

`tools/ps5to6/src/Snapshot/Snapshot.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>ps5to6-snapshot</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/src/Snapshot/Program.cs`:
```csharp
using System;
using System.IO;
using Ps5To6.Tools.Common;

// Usage: ps5to6-snapshot <solutionRootDir> <outputDir>
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ps5to6-snapshot <solutionRootDir> <outputDir>");
    return 2;
}

string root = args[0];
string outDir = args[1];
Directory.CreateDirectory(outDir);

InventoryDoc doc = Inventory.Build(root);
File.WriteAllText(Path.Combine(outDir, "inventory.json"), Inventory.ToJson(doc));
File.WriteAllText(Path.Combine(outDir, "inventory.md"), Inventory.ToMarkdown(doc));

Console.WriteLine($"Wrote inventory for {doc.Projects.Count} projects to {outDir}");
return 0;
```

Add the project to the solution:
```bash
dotnet sln tools/ps5to6/Ps5To6.Tools.sln add tools/ps5to6/src/Snapshot/Snapshot.csproj
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add snapshot tool producing inventory.json + inventory.md"
```

---

### Task 5: `uninstall-all` tool — strip all package references

**Files:**
- Create: `tools/ps5to6/src/Common/PackageStripper.cs`
- Create: `tools/ps5to6/src/UninstallAll/UninstallAll.csproj`
- Create: `tools/ps5to6/src/UninstallAll/Program.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/PackageStripperTests.cs`
- Modify: `tools/ps5to6/Ps5To6.Tools.sln`

**Interfaces:**
- Consumes: nothing (operates on raw csproj text/XML + packages.config).
- Produces:
  - `static class PackageStripper`
  - `static string StripCsproj(string csprojXml)` — returns csproj XML with all `<PackageReference>` items removed (empty `<ItemGroup>`s pruned), `<ProjectReference>` untouched.
  - `static bool ShouldRemovePackagesConfig(string csprojDir)` — true if a `packages.config` exists.
  - `static void Apply(string csprojPath, bool deletePackagesConfig)` — rewrites the file in place; deletes packages.config when asked.

- [ ] **Step 1: Write the failing tests**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/PackageStripperTests.cs`:
```csharp
using Ps5To6.Tools.Common;
using Xunit;

public class PackageStripperTests
{
    [Fact]
    public void Removes_packagereferences_keeps_projectreferences()
    {
        string xml = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Serilog" Version="3.1.1" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\Core\Core.csproj" />
          </ItemGroup>
        </Project>
        """;

        string result = PackageStripper.StripCsproj(xml);

        Assert.DoesNotContain("PackageReference", result);
        Assert.Contains("ProjectReference", result);
        Assert.Contains("net8.0", result);
    }

    [Fact]
    public void Prunes_emptied_itemgroups()
    {
        string xml = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup><PackageReference Include="A" Version="1.0.0" /></ItemGroup>
        </Project>
        """;
        string result = PackageStripper.StripCsproj(xml);
        Assert.DoesNotContain("ItemGroup", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `PackageStripper` does not exist.

- [ ] **Step 3: Implement the stripper**

`tools/ps5to6/src/Common/PackageStripper.cs`:
```csharp
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ps5To6.Tools.Common;

public static class PackageStripper
{
    public static string StripCsproj(string csprojXml)
    {
        XDocument doc = XDocument.Parse(csprojXml, LoadOptions.PreserveWhitespace);

        doc.Descendants().Where(e => e.Name.LocalName == "PackageReference")
            .ToList().ForEach(e => e.Remove());

        // Prune now-empty <ItemGroup> elements (no element children left).
        doc.Descendants().Where(e => e.Name.LocalName == "ItemGroup" && !e.Elements().Any())
            .ToList().ForEach(e => e.Remove());

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    public static bool ShouldRemovePackagesConfig(string csprojDir) =>
        File.Exists(Path.Combine(csprojDir, "packages.config"));

    public static void Apply(string csprojPath, bool deletePackagesConfig)
    {
        string stripped = StripCsproj(File.ReadAllText(csprojPath));
        File.WriteAllText(csprojPath, stripped);

        string dir = Path.GetDirectoryName(Path.GetFullPath(csprojPath))!;
        if (deletePackagesConfig && ShouldRemovePackagesConfig(dir))
            File.Delete(Path.Combine(dir, "packages.config"));
    }
}
```

`tools/ps5to6/src/UninstallAll/UninstallAll.csproj` (same shape as Snapshot.csproj; `AssemblyName` = `ps5to6-uninstall-all`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>ps5to6-uninstall-all</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/src/UninstallAll/Program.cs`:
```csharp
using System;
using System.Linq;
using Ps5To6.Tools.Common;

// Usage: ps5to6-uninstall-all <solutionRootDir> [--apply] [--keep-packages-config]
if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: ps5to6-uninstall-all <solutionRootDir> [--apply] [--keep-packages-config]");
    return 2;
}

string root = args[0];
bool apply = args.Contains("--apply");
bool deleteConfig = !args.Contains("--keep-packages-config");

var projects = SolutionScanner.ScanDirectory(root);
foreach (var p in projects)
{
    if (apply)
    {
        PackageStripper.Apply(p.Path, deleteConfig);
        Console.WriteLine($"stripped: {p.Id}");
    }
    else
    {
        Console.WriteLine($"would strip: {p.Id} ({p.Packages.Count} package refs)");
    }
}
Console.WriteLine(apply ? "Done (applied)." : "Dry run — pass --apply to write changes.");
return 0;
```

Add to solution:
```bash
dotnet sln tools/ps5to6/Ps5To6.Tools.sln add tools/ps5to6/src/UninstallAll/UninstallAll.csproj
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add uninstall-all tool to strip package references"
```

---

### Task 6: `feed-probe` tool — net8 availability matrix (offline-testable core)

**Files:**
- Create: `tools/ps5to6/src/Common/Net8VersionSelector.cs`
- Create: `tools/ps5to6/src/Common/IPackageFeed.cs`
- Create: `tools/ps5to6/src/FeedProbe/FeedProbe.csproj`
- Create: `tools/ps5to6/src/FeedProbe/NuGetPackageFeed.cs`
- Create: `tools/ps5to6/src/FeedProbe/Program.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/Net8VersionSelectorTests.cs`
- Modify: `tools/ps5to6/Ps5To6.Tools.sln`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `record PackageCandidate(string Version, IReadOnlyList<string> TargetFrameworks)` — `TargetFrameworks` are TFM short folder names (e.g. `net8.0`, `netstandard2.0`, `net472`).
  - `record FeedResult(string PackageId, bool Available, string? SelectedVersion)`.
  - `static class Net8VersionSelector { static FeedResult Select(string packageId, IReadOnlyList<PackageCandidate> candidates); }` — picks the highest version whose framework set is compatible with `net8.0` (a candidate is compatible if any of its TFMs is `net8.0`/`net8.0-*`, or `netstandard2.0`/`netstandard2.1`, or `net5.0`+ ). Highest = highest semver among compatible.
  - `interface IPackageFeed { Task<IReadOnlyList<PackageCandidate>> GetCandidatesAsync(string packageId, CancellationToken ct); }`

- [ ] **Step 1: Write the failing tests (selector — the offline core)**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/Net8VersionSelectorTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `Net8VersionSelector` / `PackageCandidate` do not exist.

- [ ] **Step 3: Implement the selector + feed interface (in Common)**

`tools/ps5to6/src/Common/Net8VersionSelector.cs`:
```csharp
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
            .OrderByDescending(c => ParseVersion(c.Version))
            .FirstOrDefault();

        return best is null
            ? new FeedResult(packageId, false, null)
            : new FeedResult(packageId, true, best.Version);
    }

    private static bool IsNet8Compatible(string tfm)
    {
        string t = tfm.Trim().ToLowerInvariant();
        if (t.StartsWith("net8.0")) return true;
        if (t is "netstandard2.0" or "netstandard2.1") return true;
        // net5.0/net6.0/net7.0 (with or without -windows) are forward-compatible to net8.
        if (t.StartsWith("net5.0") || t.StartsWith("net6.0") || t.StartsWith("net7.0")) return true;
        return false;
    }

    private static Version ParseVersion(string v)
    {
        // Strip any pre-release suffix for ordering (e.g. "2.1.0-beta" -> 2.1.0).
        string core = new string(v.TakeWhile(ch => char.IsDigit(ch) || ch == '.').ToArray());
        return Version.TryParse(core, out Version? parsed) ? parsed! : new Version(0, 0);
    }
}
```

`tools/ps5to6/src/Common/IPackageFeed.cs`:
```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ps5To6.Tools.Common;

public interface IPackageFeed
{
    Task<IReadOnlyList<PackageCandidate>> GetCandidatesAsync(string packageId, CancellationToken ct);
}
```

- [ ] **Step 4: Run selector tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS.

- [ ] **Step 5: Implement the NuGet.Protocol feed adapter + Program (not unit-tested — thin I/O)**

`tools/ps5to6/src/FeedProbe/FeedProbe.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>ps5to6-feed-probe</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NuGet.Protocol" Version="6.11.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/src/FeedProbe/NuGetPackageFeed.cs`:
```csharp
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
```

`tools/ps5to6/src/FeedProbe/Program.cs`:
```csharp
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
```

Add to solution:
```bash
dotnet sln tools/ps5to6/Ps5To6.Tools.sln add tools/ps5to6/src/FeedProbe/FeedProbe.csproj
```

- [ ] **Step 6: Build (and confirm tests still pass)**

Run: `dotnet build tools/ps5to6/Ps5To6.Tools.sln` then `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: build succeeds (NuGet.Protocol restores); selector tests PASS.

- [ ] **Step 7: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add feed-probe (offline-tested net8 version selector + NuGet adapter)"
```

---

### Task 7: `scaffold-project` tool — KB-driven SDK-style net8 csproj

**Files:**
- Create: `tools/ps5to6/src/Common/PsProjectType.cs`
- Create: `tools/ps5to6/src/Common/CsprojScaffolder.cs`
- Create: `tools/ps5to6/src/ScaffoldProject/ScaffoldProject.csproj`
- Create: `tools/ps5to6/src/ScaffoldProject/Program.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/CsprojScaffolderTests.cs`
- Modify: `tools/ps5to6/Ps5To6.Tools.sln`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `enum PsProjectType { Service, RichClient, PublishingService, Configuration }`
  - `record ScaffoldSpec(PsProjectType Type, IReadOnlyList<(string Id, string Version)> Packages)` — the resolved package set (the orchestrator passes the feed-probe-confirmed versions).
  - `static class CsprojScaffolder { static string TargetFrameworkFor(PsProjectType type); static string Build(ScaffoldSpec spec); }` where `TargetFrameworkFor(RichClient) == "net8.0-windows"` and all others `== "net8.0"`.

- [ ] **Step 1: Write the failing tests**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/CsprojScaffolderTests.cs`:
```csharp
using System.Collections.Generic;
using Ps5To6.Tools.Common;
using Xunit;

public class CsprojScaffolderTests
{
    [Fact]
    public void RichClient_targets_net8_windows()
    {
        Assert.Equal("net8.0-windows", CsprojScaffolder.TargetFrameworkFor(PsProjectType.RichClient));
        Assert.Equal("net8.0", CsprojScaffolder.TargetFrameworkFor(PsProjectType.Service));
    }

    [Fact]
    public void Build_emits_sdk_style_with_packages_and_tfm()
    {
        var spec = new ScaffoldSpec(PsProjectType.Service,
            new List<(string, string)> { ("Noxum.PS5.Service", "5.4.0"), ("Noxum.Publishing.Core", "2.1.0") });

        string xml = CsprojScaffolder.Build(spec);

        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", xml);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", xml);
        Assert.Contains("<PackageReference Include=\"Noxum.PS5.Service\" Version=\"5.4.0\" />", xml);
        Assert.Contains("<PackageReference Include=\"Noxum.Publishing.Core\" Version=\"2.1.0\" />", xml);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `CsprojScaffolder` / `PsProjectType` do not exist.

- [ ] **Step 3: Implement the scaffolder**

`tools/ps5to6/src/Common/PsProjectType.cs`:
```csharp
namespace Ps5To6.Tools.Common;

public enum PsProjectType
{
    Service,
    RichClient,
    PublishingService,
    Configuration
}
```

`tools/ps5to6/src/Common/CsprojScaffolder.cs`:
```csharp
using System.Collections.Generic;
using System.Text;

namespace Ps5To6.Tools.Common;

public record ScaffoldSpec(PsProjectType Type, IReadOnlyList<(string Id, string Version)> Packages);

public static class CsprojScaffolder
{
    public static string TargetFrameworkFor(PsProjectType type) =>
        type == PsProjectType.RichClient ? "net8.0-windows" : "net8.0";

    public static string Build(ScaffoldSpec spec)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine($"    <TargetFramework>{TargetFrameworkFor(spec.Type)}</TargetFramework>");
        if (spec.Type == PsProjectType.RichClient)
            sb.AppendLine("    <UseWindowsForms>true</UseWindowsForms>");
        sb.AppendLine("    <Nullable>disable</Nullable>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <ItemGroup>");
        foreach ((string id, string version) in spec.Packages)
            sb.AppendLine($"    <PackageReference Include=\"{id}\" Version=\"{version}\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }
}
```

`tools/ps5to6/src/ScaffoldProject/ScaffoldProject.csproj` (same shape; `AssemblyName` = `ps5to6-scaffold-project`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>ps5to6-scaffold-project</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/src/ScaffoldProject/Program.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ps5To6.Tools.Common;

// Usage: ps5to6-scaffold-project <type> <packagesJson> <outputCsprojPath>
//   <type>: Service | RichClient | PublishingService | Configuration
//   <packagesJson>: JSON array of {"id":"...","version":"..."}
if (args.Length != 3 || !Enum.TryParse(args[0], out PsProjectType type))
{
    Console.Error.WriteLine("Usage: ps5to6-scaffold-project <Service|RichClient|PublishingService|Configuration> <packagesJson> <outputCsprojPath>");
    return 2;
}

var pkgs = System.Text.Json.JsonSerializer.Deserialize<List<PkgDto>>(File.ReadAllText(args[1])) ?? new();
var spec = new ScaffoldSpec(type, pkgs.Select(p => (p.Id, p.Version)).ToList());
File.WriteAllText(args[2], CsprojScaffolder.Build(spec));
Console.WriteLine($"Scaffolded {type} csproj -> {args[2]}");
return 0;

record PkgDto(string Id, string Version);
```

Add to solution:
```bash
dotnet sln tools/ps5to6/Ps5To6.Tools.sln add tools/ps5to6/src/ScaffoldProject/ScaffoldProject.csproj
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/ps5to6
git commit -m "feat(tools): add scaffold-project tool for SDK-style net8 csproj generation"
```

---

### Task 8: `report` tool — aggregate run-folder ledgers into report.md

**Files:**
- Create: `tools/ps5to6/src/Common/RunReport.cs`
- Create: `tools/ps5to6/src/Report/Report.csproj`
- Create: `tools/ps5to6/src/Report/Program.cs`
- Create: `tools/ps5to6/tests/Ps5To6.Tools.Tests/RunReportTests.cs`
- Modify: `tools/ps5to6/Ps5To6.Tools.sln`

**Interfaces:**
- Consumes: nothing (reads a structured `RunStatus` it is given).
- Produces:
  - `enum ProjectOutcome { Raised, Partial, Blocked }`
  - `record ProjectStatus(string ProjectId, ProjectOutcome Outcome, string? Note)`
  - `record RunStatus(IReadOnlyList<ProjectStatus> Projects, IReadOnlyList<string> UnmappedNoxumPackages, IReadOnlyList<string> MissingNet8Dependencies)`
  - `static class RunReport { static string Render(RunStatus status); }`

- [ ] **Step 1: Write the failing test**

`tools/ps5to6/tests/Ps5To6.Tools.Tests/RunReportTests.cs`:
```csharp
using System.Collections.Generic;
using Ps5To6.Tools.Common;
using Xunit;

public class RunReportTests
{
    [Fact]
    public void Render_summarizes_outcomes_and_gaps()
    {
        var status = new RunStatus(
            new List<ProjectStatus>
            {
                new("Core", ProjectOutcome.Raised, null),
                new("App", ProjectOutcome.Blocked, "Noxum.Foo has no net8 build"),
            },
            new List<string> { "Noxum.Foo" },
            new List<string> { "SomeThirdParty 1.2.3" });

        string md = RunReport.Render(status);

        Assert.Contains("# PS5→PS6 — Migration Report", md);
        Assert.Contains("Raised: 1", md);
        Assert.Contains("Blocked: 1", md);
        Assert.Contains("Noxum.Foo", md);
        Assert.Contains("SomeThirdParty 1.2.3", md);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: FAIL — `RunReport` does not exist.

- [ ] **Step 3: Implement the report renderer**

`tools/ps5to6/src/Common/RunReport.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ps5To6.Tools.Common;

public enum ProjectOutcome { Raised, Partial, Blocked }

public record ProjectStatus(string ProjectId, ProjectOutcome Outcome, string? Note);

public record RunStatus(
    IReadOnlyList<ProjectStatus> Projects,
    IReadOnlyList<string> UnmappedNoxumPackages,
    IReadOnlyList<string> MissingNet8Dependencies);

public static class RunReport
{
    public static string Render(RunStatus status)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PS5→PS6 — Migration Report");
        sb.AppendLine();
        int raised = status.Projects.Count(p => p.Outcome == ProjectOutcome.Raised);
        int partial = status.Projects.Count(p => p.Outcome == ProjectOutcome.Partial);
        int blocked = status.Projects.Count(p => p.Outcome == ProjectOutcome.Blocked);
        sb.AppendLine($"Raised: {raised} · Partial: {partial} · Blocked: {blocked}");
        sb.AppendLine();
        sb.AppendLine("## Per-project outcome");
        sb.AppendLine();
        sb.AppendLine("| Project | Outcome | Note |");
        sb.AppendLine("|---|---|---|");
        foreach (ProjectStatus p in status.Projects)
            sb.AppendLine($"| {p.ProjectId} | {p.Outcome} | {p.Note ?? ""} |");
        sb.AppendLine();
        sb.AppendLine("## Unmapped Noxum packages (no net8 successor found)");
        sb.AppendLine();
        foreach (string pkg in status.UnmappedNoxumPackages) sb.AppendLine($"- {pkg}");
        sb.AppendLine();
        sb.AppendLine("## Missing non-Noxum dependencies (no net8 build)");
        sb.AppendLine();
        foreach (string dep in status.MissingNet8Dependencies) sb.AppendLine($"- {dep}");
        return sb.ToString();
    }
}
```

`tools/ps5to6/src/Report/Report.csproj` (same shape; `AssemblyName` = `ps5to6-report`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>ps5to6-report</AssemblyName>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Common\Ps5To6.Tools.Common.csproj" />
  </ItemGroup>
</Project>
```

`tools/ps5to6/src/Report/Program.cs`:
```csharp
using System;
using System.IO;
using Ps5To6.Tools.Common;

// Usage: ps5to6-report <runStatusJson> <outputMarkdown>
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ps5to6-report <runStatusJson> <outputMarkdown>");
    return 2;
}

RunStatus status = System.Text.Json.JsonSerializer.Deserialize<RunStatus>(
    File.ReadAllText(args[0]),
    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Could not parse run status JSON.");

File.WriteAllText(args[1], RunReport.Render(status));
Console.WriteLine($"Wrote report -> {args[1]}");
return 0;
```

Add to solution:
```bash
dotnet sln tools/ps5to6/Ps5To6.Tools.sln add tools/ps5to6/src/Report/Report.csproj
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tools/ps5to6/Ps5To6.Tools.sln`
Expected: PASS — all tests across the suite green.

- [ ] **Step 5: Final full build + commit**

Run: `dotnet build tools/ps5to6/Ps5To6.Tools.sln -c Release`
Expected: Release build of all five tools + library succeeds.

```bash
git add tools/ps5to6
git commit -m "feat(tools): add report tool aggregating run status into report.md"
```

---

## Self-Review

**Spec coverage:**
- §2 tool 1 `snapshot` → Tasks 2,3,4 (parse, graph, inventory). ✓
- §2 tool 2 `uninstall-all` → Task 5. ✓
- §2 tool 3 `feed-probe` → Task 6 (offline-tested selector + NuGet adapter). ✓
- §2 tool 4 `scaffold-project` → Task 7. ✓
- §2 tool 5 `report` → Task 8. ✓
- Classification rule (dependency-only vs code) → Task 2 `IsDependencyOnly`. ✓
- Bottom-up order → Task 3. ✓
- Single-file publishable → `PublishSingleFile=true` in every tool csproj. ✓
- xUnit fixtures (legacy packages.config, SDK-style, dependency-only, mini graph, offline feed) → Task 2 fixtures + Task 6 selector candidates (offline). ✓
- Microsoft/System packages flagged but recorded → Task 2 `Make()` `IsMicrosoftOrSystem`. ✓

**Deferred to Plan B (not this plan):** the migration KB content, the skill, the agents, smoke-check/grant-permissions/AGENTS/README/SETUP, bootstrap.ps1 extension, and the `dist/` publishing of single-file exes. Plan B consumes the tool assembly names defined here: `ps5to6-snapshot`, `ps5to6-uninstall-all`, `ps5to6-feed-probe`, `ps5to6-scaffold-project`, `ps5to6-report`.

**Placeholder scan:** no TBD/TODO; every code step shows complete code. ✓

**Type consistency:** `ProjectInfo`/`PackageRef` (Task 2) reused unchanged in Tasks 3,4,5; `PackageCandidate`/`FeedResult` (Task 6) consistent; `ScaffoldSpec` (Task 7) and `RunStatus` (Task 8) self-contained. ✓

**Note for the live feed:** the `feed-probe` adapter (`NuGetPackageFeed`) is intentionally not unit-tested (it is thin I/O against a live feed, which the dogma forbids testing here). All decision logic is in the offline-tested `Net8VersionSelector`.
