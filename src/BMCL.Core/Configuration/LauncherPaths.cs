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

        Root = Path.GetFullPath(root);
        ConfigRoot = Path.GetFullPath(configRoot ?? root);
    }

    public string Root { get; }

    public string ConfigRoot { get; }

    public string ConfigFile => Path.Combine(ConfigRoot, "config.json");

    public string MinecraftRoot => Path.Combine(Root, ".minecraft");

    public string Versions => Path.Combine(MinecraftRoot, "versions");

    public string Libraries => Path.Combine(MinecraftRoot, "libraries");

    public string Assets => Path.Combine(MinecraftRoot, "assets");

    public string AssetIndexes => Path.Combine(Assets, "indexes");

    public string AssetObjects => Path.Combine(Assets, "objects");

    public string JavaRuntimes => Path.Combine(Root, "java");

    public string Logs => Path.Combine(Root, "logs");

    public string GetInstanceDirectory(Instance instance) =>
        instance.Isolated ? Path.Combine(Versions, instance.Id) : MinecraftRoot;

    public static LauncherPaths CreateDefault()
    {
        var root = GetDefaultRoot();
        return new LauncherPaths(root, root);
    }

    public static string GetDefaultRoot()
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
                     Root,
                     ConfigRoot,
                     MinecraftRoot,
                     Versions,
                     Libraries,
                     Assets,
                     AssetIndexes,
                     AssetObjects,
                     JavaRuntimes,
                     Logs,
                 })
        {
            Directory.CreateDirectory(directory);
        }
    }
}
