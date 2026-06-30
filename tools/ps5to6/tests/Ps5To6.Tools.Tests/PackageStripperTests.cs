using Ps5To6.Tools.Common;
using Xunit;

public class PackageStripperTests
{
    [Fact]
    public void Removes_packagereferences_keeps_projectreferences()
    {
        string xml = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Serilog" Version="3.1.1" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\Core\Core.csproj" />
          </ItemGroup>
        </Project>
        """;

        string result = PackageStripper.StripCsproj(xml);

        Assert.DoesNotContain("PackageReference", result);
        Assert.Contains("ProjectReference", result);
        Assert.Contains("net8.0", result);
    }

    [Fact]
    public void Prunes_emptied_itemgroups()
    {
        string xml = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup><PackageReference Include="A" Version="1.0.0" /></ItemGroup>
        </Project>
        """;
        string result = PackageStripper.StripCsproj(xml);
        Assert.DoesNotContain("ItemGroup", result);
    }
}
