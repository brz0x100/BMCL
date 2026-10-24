using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using Xunit;

namespace BMCL.Tests;

public sealed class InstanceStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "bmcl-instance-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadRoundTripsInstance()
    {
        var paths = new LauncherPaths(_root);
        var store = new InstanceStore(paths);
        var instance = new Instance
        {
            Id = "survival",
            Name = "Survival",
            VersionId = "1.21.4",
            CreatedAt = DateTimeOffset.UtcNow,
            LastPlayedAt = DateTimeOffset.UtcNow,
            JavaPath = "/usr/lib/jvm/java-21/bin/java",
            MemoryMb = 4096,
            Isolated = false,
        };

        await store.SaveAsync(instance);
        var loaded = Assert.Single(await store.LoadAsync());

        Assert.Equal(instance, loaded);
        Assert.True(File.Exists(Path.Combine(paths.Versions, "survival", "instance.json")));
    }

    [Fact]
    public async Task LoadSortsByMostRecentlyPlayed()
    {
        var store = new InstanceStore(new LauncherPaths(_root));
        var older = new Instance
        {
            Id = "older",
            Name = "Older",
            VersionId = "1.20.4",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            LastPlayedAt = DateTimeOffset.UtcNow.AddDays(-2),
        };
        var newer = new Instance
        {
            Id = "newer",
            Name = "Newer",
            VersionId = "1.21.4",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastPlayedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        await store.SaveAsync(older);
        await store.SaveAsync(newer);
        var loaded = await store.LoadAsync();

        Assert.Equal(["newer", "older"], loaded.Select(instance => instance.Id));
    }

    [Fact]
    public async Task LoadsStandardVersionFolderWithoutMetadata()
    {
        var paths = new LauncherPaths(_root);
        var versionDirectory = Path.Combine(paths.Versions, "1.20.1");
        Directory.CreateDirectory(versionDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "1.20.1.json"),
            """{"id":"1.20.1"}""");
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "options.txt"),
            "fov:0");

        var loaded = Assert.Single(await new InstanceStore(paths).LoadAsync());

        Assert.Equal("1.20.1", loaded.Id);
        Assert.Equal("1.20.1", loaded.VersionId);
        Assert.True(loaded.Isolated);
    }

    [Fact]
    public async Task SaveRejectsPathTraversalIds()
    {
        var store = new InstanceStore(new LauncherPaths(_root));
        var instance = new Instance
        {
            Id = "../escape",
            Name = "Invalid",
            VersionId = "1.21.4",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(instance));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
