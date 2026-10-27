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
    public void PreservesBackslashesInWindowsPaths()
    {
        Assert.Equal(
            new[] { "--gameDir", @"C:\Games\.minecraft" },
            ArgumentTokenizer.Split(@"--gameDir C:\Games\.minecraft"));
    }

    [Fact]
    public void PreservesEmptyQuotedArguments()
    {
        Assert.Equal(
            new[] { "--title", "" },
            ArgumentTokenizer.Split("--title \"\""));
    }

    [Fact]
    public void SupportsEscapedQuotes()
    {
        Assert.Equal(
            new[] { "--title", "Hello \"World\"" },
            ArgumentTokenizer.Split("--title \"Hello \\\"World\\\"\""));
    }

    [Fact]
    public void ReturnsEmptyForBlankInput()
    {
        Assert.Empty(ArgumentTokenizer.Split("   "));
    }
}
