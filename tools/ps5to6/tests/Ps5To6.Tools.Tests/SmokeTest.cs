using Ps5To6.Tools.Common;
using Xunit;

namespace Ps5To6.Tools.Tests;

public class SmokeTest
{
    [Fact]
    public void Library_is_referenced()
    {
        Assert.Equal("ps5to6", Placeholder.Marker());
    }
}
