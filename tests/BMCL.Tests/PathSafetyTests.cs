using BMCL.Core.IO;
using Xunit;

namespace BMCL.Tests;

public sealed class PathSafetyTests
{
    [Theory]
    [InlineData("1.20.1")]
    [InlineData("fabric-loader-0.15.0")]
    [InlineData("a..b")]
    public void AcceptsSafeFileNames(string value)
    {
        Assert.True(PathSafety.IsValidFileName(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void RejectsUnsafeFileNames(string value)
    {
        Assert.False(PathSafety.IsValidFileName(value));
    }

    [Fact]
    public void CombinesRelativePathUnderRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "bmcl-path-safety");

        Assert.Equal(
            Path.GetFullPath(Path.Combine(root, "libraries", "demo.jar")),
            PathSafety.CombineUnderRoot(root, "libraries/demo.jar"));
    }

    [Theory]
    [InlineData("../escape.jar")]
    [InlineData("nested/../../escape.jar")]
    public void RejectsPathTraversal(string value)
    {
        var root = Path.Combine(Path.GetTempPath(), "bmcl-path-safety");

        Assert.Throws<InvalidDataException>(() => PathSafety.CombineUnderRoot(root, value));
    }

    [Fact]
    public void RejectsRootedRelativePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "bmcl-path-safety");
        var escape = Path.Combine(Path.GetTempPath(), "escape.jar");

        Assert.Throws<InvalidDataException>(() => PathSafety.CombineUnderRoot(root, escape));
    }
}
