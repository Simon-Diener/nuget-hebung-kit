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
