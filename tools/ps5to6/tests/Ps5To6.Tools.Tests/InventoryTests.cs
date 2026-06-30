using System;
using System.IO;
using System.Linq;
using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class InventoryTests
{
    // Resolve the source fixtures directory rather than the copy under bin/:
    // SolutionScanner intentionally skips any path containing bin/ or obj/, so a
    // fixtures tree living under the test's bin output would be filtered out.
    private static string FixturesRoot
    {
        get
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "fixtures");
                if (Directory.Exists(candidate) &&
                    !dir.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate the source 'fixtures' directory.");
        }
    }

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
