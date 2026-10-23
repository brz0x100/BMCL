using BMCL.Core.Minecraft;
using Xunit;

namespace BMCL.Tests;

public sealed class MavenPathTests
{
    [Fact]
    public void ResolvesMavenCoordinates()
    {
        Assert.Equal(
            "org/lwjgl/lwjgl/3.3.1/lwjgl-3.3.1.jar",
            MavenPath.ToRelativePath("org.lwjgl:lwjgl:3.3.1"));
    }

    [Fact]
    public void ResolvesClassifierAndExtension()
    {
        Assert.Equal(
            "org/lwjgl/lwjgl/3.3.1/lwjgl-3.3.1-natives-linux.jar",
            MavenPath.ToRelativePath("org.lwjgl:lwjgl:3.3.1:natives-linux"));

        Assert.Equal(
            "com/example/demo/1.0/demo-1.0.zip",
            MavenPath.ToRelativePath("com.example:demo:1.0@zip"));
    }

    [Fact]
    public void ReturnsNullForInvalidCoordinates()
    {
        Assert.Null(MavenPath.ToRelativePath("invalid"));
    }
}
