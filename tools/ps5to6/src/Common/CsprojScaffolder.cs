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
