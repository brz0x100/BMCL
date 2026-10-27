using BMCL.Core.Configuration;
using Xunit;

namespace BMCL.Tests;

public sealed class LauncherSettingsStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "bmcl-settings-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadRoundTripsSettings()
    {
        var paths = new LauncherPaths(_root);
        var store = new LauncherSettingsStore(paths);
        var settings = new LauncherSettings
        {
            JavaPath = "/usr/lib/jvm/java-21/bin/java",
            MinMemoryMb = 1024,
            MaxMemoryMb = 8192,
            MaxConcurrentDownloads = 12,
            UseBmclApi = true,
            Theme = LauncherTheme.Dark,
            DefaultIsolationStrategy = IsolationStrategy.Always,
            SelectedInstanceId = "survival",
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void SettingsFileUsesSeparateConfigDirectory()
    {
        var dataRoot = Path.Combine(_root, "data");
        var configRoot = Path.Combine(_root, "config");
        var store = new LauncherSettingsStore(new LauncherPaths(dataRoot, configRoot));

        Assert.Equal(
            Path.Combine(Path.GetFullPath(configRoot), "config.json"),
            store.FilePath);
    }

    [Fact]
    public async Task SaveWritesConfigurationToConfigDirectoryOnly()
    {
        var dataRoot = Path.Combine(_root, "data");
        var configRoot = Path.Combine(_root, "config");
        var store = new LauncherSettingsStore(new LauncherPaths(dataRoot, configRoot));

        await store.SaveAsync(new LauncherSettings { Theme = LauncherTheme.Light });

        Assert.True(File.Exists(store.FilePath));
        Assert.False(File.Exists(Path.Combine(dataRoot, "config.json")));
        var loaded = await store.LoadAsync();
        Assert.Equal(LauncherTheme.Light, loaded.Theme);
    }

    [Fact]
    public async Task LoadsLegacyConfigurationWhenNewConfigIsMissing()
    {
        var dataRoot = Path.Combine(_root, "data");
        var configRoot = Path.Combine(_root, "config");
        Directory.CreateDirectory(dataRoot);
        await File.WriteAllTextAsync(
            Path.Combine(dataRoot, "config.json"),
            """{"Theme":"Light"}""");

        var store = new LauncherSettingsStore(new LauncherPaths(dataRoot, configRoot));
        var loaded = await store.LoadAsync();

        Assert.Equal(LauncherTheme.Light, loaded.Theme);
    }

    [Fact]
    public async Task LoadReturnsDefaultsWhenFileDoesNotExist()
    {
        var store = new LauncherSettingsStore(new LauncherPaths(_root));

        var loaded = await store.LoadAsync();

        Assert.Null(loaded.JavaPath);
        Assert.Equal(512, loaded.MinMemoryMb);
        Assert.Equal(4096, loaded.MaxMemoryMb);
        Assert.Equal(8, loaded.MaxConcurrentDownloads);
        Assert.False(loaded.UseBmclApi);
        Assert.Equal(LauncherTheme.System, loaded.Theme);
        Assert.Equal(IsolationStrategy.Modded, loaded.DefaultIsolationStrategy);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
