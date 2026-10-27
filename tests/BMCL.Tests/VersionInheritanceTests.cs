using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.Minecraft;
using BMCL.Download;
using Xunit;

namespace BMCL.Tests;

public sealed class VersionInheritanceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "bmcl-inheritance-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ChildOverridesAndExtendsParent()
    {
        var parent = CreateParent();
        var child = new VersionDetail
        {
            Id = "fabric-loader",
            Type = "release",
            MainClass = "net.fabricmc.loader.impl.launch.knot.KnotClient",
            InheritsFrom = "1.21.4",
            Libraries =
            [
                new MinecraftLibrary
                {
                    Name = "com.example:demo:1.0",
                    Downloads = new LibraryDownloads
                    {
                        Artifact = new DownloadArtifact { Path = "patched/demo.jar" },
                    },
                },
                new MinecraftLibrary { Name = "net.fabricmc:fabric-loader:0.15.0" },
            ],
            Arguments = new VersionArguments
            {
                Game = [new VersionArgument(["--fabric"], [])],
            },
        };

        var merged = VersionDetailMerger.Merge(parent, child);

        Assert.Equal("fabric-loader", merged.Id);
        Assert.Equal("net.fabricmc.loader.impl.launch.knot.KnotClient", merged.MainClass);
        Assert.Equal(parent.AssetIndex, merged.AssetIndex);
        Assert.Equal(parent.Downloads, merged.Downloads);
        Assert.Equal(parent.JavaVersion, merged.JavaVersion);
        Assert.Equal(3, merged.Libraries.Count);
        Assert.Equal("patched/demo.jar", merged.Libraries[0].Downloads!.Artifact!.Path);
        Assert.Equal("org.example:lwjgl:3.3.1", merged.Libraries[1].Name);
        Assert.Equal("net.fabricmc:fabric-loader:0.15.0", merged.Libraries[2].Name);
        Assert.Equal(2, merged.Arguments!.Game.Count);
        Assert.Single(merged.Arguments.Jvm);
    }

    [Fact]
    public async Task ServiceResolvesCachedParentVersion()
    {
        var paths = new LauncherPaths(_root);
        paths.EnsureCreated();
        await WriteVersionAsync(paths, CreateParent());
        await WriteVersionAsync(paths, new VersionDetail
        {
            Id = "fabric-loader",
            Type = "release",
            MainClass = "net.fabricmc.loader.impl.launch.knot.KnotClient",
            InheritsFrom = "1.21.4",
            Libraries = [new MinecraftLibrary { Name = "net.fabricmc:fabric-loader:0.15.0" }],
        });

        var service = new MinecraftVersionService(
            new HttpClient(),
            paths,
            new IdentityMirrorUrlMapper());

        var version = await service.GetVersionAsync("fabric-loader");

        Assert.Equal("fabric-loader", version.Id);
        Assert.Equal("1.21.4", version.InheritsFrom);
        Assert.Equal("net.fabricmc.loader.impl.launch.knot.KnotClient", version.MainClass);
        Assert.Equal(3, version.Libraries.Count);
        Assert.Equal("5", version.AssetIndex!.Id);
    }

    [Fact]
    public async Task ServiceRejectsInheritanceCycles()
    {
        var paths = new LauncherPaths(_root);
        paths.EnsureCreated();
        await WriteVersionAsync(paths, new VersionDetail
        {
            Id = "cycle-a",
            InheritsFrom = "cycle-b",
        });
        await WriteVersionAsync(paths, new VersionDetail
        {
            Id = "cycle-b",
            InheritsFrom = "cycle-a",
        });

        var service = new MinecraftVersionService(
            new HttpClient(),
            paths,
            new IdentityMirrorUrlMapper());

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetVersionAsync("cycle-a"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static VersionDetail CreateParent() => new()
    {
        Id = "1.21.4",
        Type = "release",
        MainClass = "net.minecraft.client.main.Main",
        AssetIndex = new AssetIndex { Id = "5", Url = "https://example.test/5.json" },
        Downloads = new VersionDownloads
        {
            Client = new DownloadArtifact { Url = "https://example.test/client.jar" },
        },
        Libraries =
        [
            new MinecraftLibrary
            {
                Name = "com.example:demo:1.0",
                Downloads = new LibraryDownloads
                {
                    Artifact = new DownloadArtifact { Path = "demo/demo.jar" },
                },
            },
            new MinecraftLibrary { Name = "org.example:lwjgl:3.3.1" },
        ],
        Arguments = new VersionArguments
        {
            Game = [new VersionArgument(["--username"], [])],
            Jvm = [new VersionArgument(["-Xmx2G"], [])],
        },
        JavaVersion = new JavaVersion { MajorVersion = 21 },
    };

    private static async Task WriteVersionAsync(LauncherPaths paths, VersionDetail version)
    {
        var directory = Path.Combine(paths.VersionsDirectory, version.Id);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            Path.Combine(directory, $"{version.Id}.json"),
            JsonSerializer.Serialize(version, MinecraftJson.Options));
    }
}
