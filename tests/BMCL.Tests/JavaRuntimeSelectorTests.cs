using BMCL.Launch.Java;
using Xunit;

namespace BMCL.Tests;

public sealed class JavaRuntimeSelectorTests
{
    [Fact]
    public void SelectsLowestCompatibleMajorVersion()
    {
        JavaRuntime[] runtimes =
        [
            new("/java/8/bin/java", "/java/8", "1.8.0", 8, "x86_64", null),
            new("/java/21/bin/java", "/java/21", "21.0.1", 21, "x86_64", null),
            new("/java/17/bin/java", "/java/17", "17.0.9", 17, "x86_64", null),
        ];

        var selected = JavaRuntimeSelector.SelectCompatible(runtimes, 17);

        Assert.NotNull(selected);
        Assert.Equal(17, selected.MajorVersion);
    }

    [Fact]
    public void ReturnsNullWhenNoRuntimeMatches()
    {
        JavaRuntime[] runtimes =
        [
            new("/java/8/bin/java", "/java/8", "1.8.0", 8, "x86_64", null),
        ];

        Assert.Null(JavaRuntimeSelector.SelectCompatible(runtimes, 17));
    }
}
