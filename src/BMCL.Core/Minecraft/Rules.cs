namespace BMCL.Core.Minecraft;

public sealed record Rule
{
    public string Action { get; init; } = "allow";

    public RuleOs? Os { get; init; }

    public IReadOnlyDictionary<string, bool>? Features { get; init; }
}

public sealed record RuleOs
{
    public string? Name { get; init; }

    public string? Version { get; init; }

    public string? Arch { get; init; }
}

public sealed record RuleContext
{
    public RuleContext(
        string osName,
        string osArch,
        string? osVersion = null,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        OsName = osName;
        OsArch = osArch;
        OsVersion = osVersion;
        Features = features;
    }

    public string OsName { get; }

    public string OsArch { get; }

    public string? OsVersion { get; }

    public IReadOnlyDictionary<string, bool>? Features { get; }

    public static RuleContext Current { get; } = new(
        RuleEvaluator.GetCurrentOsName(),
        RuleEvaluator.GetCurrentOsArch(),
        Environment.OSVersion.Version.ToString(),
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["is_demo_user"] = false,
            ["has_custom_resolution"] = false,
            ["has_quick_plays_support"] = false,
            ["is_quick_play_singleplayer"] = false,
            ["is_quick_play_multiplayer"] = false,
            ["is_quick_play_realms"] = false,
        });
}
