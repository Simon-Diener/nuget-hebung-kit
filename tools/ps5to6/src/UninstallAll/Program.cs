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
