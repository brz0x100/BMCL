using BMCL.Core.Minecraft;
using Xunit;

namespace BMCL.Tests;

public sealed class LibraryFileResolverTests
{
    [Fact]
    public void ResolvesExplicitArtifactPath()
    {
        var library = new MinecraftLibrary
        {
            Name = "org.example:demo:1.0",
            Downloads = new LibraryDownloads
            {
                Artifact = new DownloadArtifact
                {
                    Path = "custom/demo.jar",
                },
            },
        };

        var file = LibraryFileResolver.ResolveArtifact(library);

        Assert.NotNull(file);
        Assert.Equal("custom/demo.jar", file.RelativePath);
        Assert.Same(library.Downloads.Artifact, file.Artifact);
    }

    [Fact]
    public void FallsBackToMavenCoordinates()
    {
        var library = new MinecraftLibrary
        {
            Name = "org.example:demo:1.0",
        };

        var file = LibraryFileResolver.ResolveArtifact(library);

        Assert.NotNull(file);
        Assert.Equal("org/example/demo/1.0/demo-1.0.jar", file.RelativePath);
        Assert.Null(file.Artifact);
    }

    [Fact]
    public void ResolvesNativeClassifier()
    {
        var library = new MinecraftLibrary
        {
            Name = "org.example:native:1.0",
            Natives = new Dictionary<string, string>
            {
                ["linux"] = "natives-linux",
            },
        };

        var file = LibraryFileResolver.ResolveNative(library, "linux");

        Assert.NotNull(file);
        Assert.Equal(
            "org/example/native/1.0/native-1.0-natives-linux.jar",
            file.RelativePath);
    }

    [Fact]
    public void ReturnsNullWhenNoNativeMatches()
    {
        var library = new MinecraftLibrary
        {
            Name = "org.example:native:1.0",
            Natives = new Dictionary<string, string>
            {
                ["windows"] = "natives-windows",
            },
        };

        Assert.Null(LibraryFileResolver.ResolveNative(library, "linux"));
    }
}
