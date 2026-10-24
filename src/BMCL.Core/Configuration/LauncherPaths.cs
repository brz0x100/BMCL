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

    public string Versions => Path.Combine(Root, "versions");

    public string Libraries => Path.Combine(Root, "libraries");

    public string Assets => Path.Combine(Root, "assets");

    public string AssetIndexes => Path.Combine(Assets, "indexes");

    public string AssetObjects => Path.Combine(Assets, "objects");

    public string Instances => Path.Combine(Root, "instances");

    public string JavaRuntimes => Path.Combine(Root, "java");

    public string Logs => Path.Combine(Root, "logs");

    public static LauncherPaths CreateDefault() => new(GetDefaultRoot(), GetDefaultConfigRoot());

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

    public static string GetDefaultConfigRoot()
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

        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
        {
            configHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");
        }

        return Path.Combine(configHome, "BMCL");
    }

    public void EnsureCreated()
    {
        foreach (var directory in new[]
                 {
                     Root,
                     ConfigRoot,
                     Versions,
                     Libraries,
                     Assets,
                     AssetIndexes,
                     AssetObjects,
                     Instances,
                     JavaRuntimes,
                     Logs,
                 })
        {
            Directory.CreateDirectory(directory);
        }
    }
}
