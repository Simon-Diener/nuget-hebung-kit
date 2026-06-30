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
