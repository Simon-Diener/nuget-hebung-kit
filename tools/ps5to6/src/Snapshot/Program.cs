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
