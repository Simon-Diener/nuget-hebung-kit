using System.Collections.Generic;
using Ps5To6.Tools.Common;
using Xunit;

public class CsprojScaffolderTests
{
    [Fact]
    public void RichClient_targets_net8_windows()
    {
        Assert.Equal("net8.0-windows", CsprojScaffolder.TargetFrameworkFor(PsProjectType.RichClient));
        Assert.Equal("net8.0", CsprojScaffolder.TargetFrameworkFor(PsProjectType.Service));
    }

    [Fact]
    public void Build_emits_sdk_style_with_packages_and_tfm()
    {
        var spec = new ScaffoldSpec(PsProjectType.Service,
            new List<(string, string)> { ("Noxum.PS5.Service", "5.4.0"), ("Noxum.Publishing.Core", "2.1.0") });

        string xml = CsprojScaffolder.Build(spec);

        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", xml);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", xml);
        Assert.Contains("<PackageReference Include=\"Noxum.PS5.Service\" Version=\"5.4.0\" />", xml);
        Assert.Contains("<PackageReference Include=\"Noxum.Publishing.Core\" Version=\"2.1.0\" />", xml);
    }
}
