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
