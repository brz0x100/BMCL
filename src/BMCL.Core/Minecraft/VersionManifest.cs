namespace BMCL.Core.Minecraft;

public sealed record VersionManifest
{
    public VersionManifestLatest? Latest { get; init; }

    public IReadOnlyList<VersionSummary> Versions { get; init; } = [];
}

public sealed record VersionManifestLatest
{
    public string Release { get; init; } = "";

    public string Snapshot { get; init; } = "";
}

public sealed record VersionSummary
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "";

    public string Url { get; init; } = "";

    public DateTimeOffset Time { get; init; }

    public DateTimeOffset ReleaseTime { get; init; }

    public string? Sha1 { get; init; }

    public int? ComplianceLevel { get; init; }
}
