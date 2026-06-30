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
