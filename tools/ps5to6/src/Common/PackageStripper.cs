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
