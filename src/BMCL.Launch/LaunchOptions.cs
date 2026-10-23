using BMCL.Auth;

namespace BMCL.Launch;

public sealed record LaunchOptions
{
    public required MinecraftAccount Account { get; init; }

    public required string GameDirectory { get; init; }

    public required string AssetsDirectory { get; init; }

    public required string LibrariesDirectory { get; init; }

    public required string NativesDirectory { get; init; }

    public required string ClientJarPath { get; init; }

    public required string JavaPath { get; init; }

    public string VersionName { get; init; } = "";

    public string? AssetIndexName { get; init; }

    public string? LoggingConfigurationPath { get; init; }

    public int MaxMemoryMb { get; init; } = 2048;

    public int? MinMemoryMb { get; init; }

    public int? WindowWidth { get; init; }

    public int? WindowHeight { get; init; }

    public string LauncherName { get; init; } = "BMCL";

    public string LauncherVersion { get; init; } = "0.1.0";

    public IReadOnlyList<string> ExtraJvmArguments { get; init; } = [];

    public IReadOnlyDictionary<string, bool>? Features { get; init; }
}

public sealed record LaunchCommand(
    string JavaPath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory);
