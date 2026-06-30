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
