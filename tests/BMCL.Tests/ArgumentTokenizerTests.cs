using BMCL.Launch;
using Xunit;

namespace BMCL.Tests;

public sealed class ArgumentTokenizerTests
{
    [Fact]
    public void SplitsOnWhitespace()
    {
        Assert.Equal(
            new[] { "--username", "Steve", "--gameDir", "/tmp/game" },
            ArgumentTokenizer.Split("--username Steve --gameDir /tmp/game"));
    }

    [Fact]
    public void KeepsQuotedValuesTogether()
    {
        Assert.Equal(
            new[] { "--title", "Hello World", "--demo" },
            ArgumentTokenizer.Split("--title \"Hello World\" --demo"));
    }

    [Fact]
    public void ReturnsEmptyForBlankInput()
    {
        Assert.Empty(ArgumentTokenizer.Split("   "));
    }
}
