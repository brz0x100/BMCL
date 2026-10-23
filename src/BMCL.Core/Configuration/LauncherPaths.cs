namespace BMCL.Core.Configuration;

public sealed class LauncherPaths
{
    public LauncherPaths(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The launcher root directory is required.", nameof(root));
        }

        Root = Path.GetFullPath(root);
    }

    public string Root { get; }

    public string Versions => Path.Combine(Root, "versions");

    public string Libraries => Path.Combine(Root, "libraries");

    public string Assets => Path.Combine(Root, "assets");

    public string AssetIndexes => Path.Combine(Assets, "indexes");

    public string AssetObjects => Path.Combine(Assets, "objects");

    public string Instances => Path.Combine(Root, "instances");

    public string JavaRuntimes => Path.Combine(Root, "java");

    public string Logs => Path.Combine(Root, "logs");

    public static LauncherPaths CreateDefault() => new(GetDefaultRoot());

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
