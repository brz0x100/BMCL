using BMCL.Core.Instances;

namespace BMCL.Core.Configuration;

public sealed class LauncherPaths
{
    public LauncherPaths(string root, string? configRoot = null)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The launcher root directory is required.", nameof(root));
        }

        RootDirectory = Path.GetFullPath(root);
        ConfigDirectory = Path.GetFullPath(configRoot ?? root);
    }

    public string RootDirectory { get; }

    public string ConfigDirectory { get; }

    public string ConfigFilePath => Path.Combine(ConfigDirectory, "config.json");

    public string MinecraftDirectory => Path.Combine(RootDirectory, ".minecraft");

    public string VersionsDirectory => Path.Combine(MinecraftDirectory, "versions");

    public string LibrariesDirectory => Path.Combine(MinecraftDirectory, "libraries");

    public string AssetsDirectory => Path.Combine(MinecraftDirectory, "assets");

    public string AssetIndexesDirectory => Path.Combine(AssetsDirectory, "indexes");

    public string AssetObjectsDirectory => Path.Combine(AssetsDirectory, "objects");

    public string JavaRuntimesDirectory => Path.Combine(RootDirectory, "java");

    public string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public string GetInstanceDirectory(Instance instance) =>
        instance.Isolated ? Path.Combine(VersionsDirectory, instance.Id) : MinecraftDirectory;

    public static LauncherPaths CreateDefault()
    {
        var root = GetDefaultRootDirectory();
        return new LauncherPaths(root, root);
    }

    public static string GetDefaultRootDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BMCL");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "BMCL");
        }

        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrWhiteSpace(dataHome))
        {
            dataHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        return Path.Combine(dataHome, "BMCL");
    }

    public void EnsureCreated()
    {
        foreach (var directory in new[]
                 {
                     RootDirectory,
                     ConfigDirectory,
                     MinecraftDirectory,
                     VersionsDirectory,
                     LibrariesDirectory,
                     AssetsDirectory,
                     AssetIndexesDirectory,
                     AssetObjectsDirectory,
                     JavaRuntimesDirectory,
                     LogsDirectory,
                 })
        {
            Directory.CreateDirectory(directory);
        }
    }
}
