namespace BMCL.Core.Instances;

public sealed record Instance
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string VersionId { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastPlayedAt { get; init; }

    public string? JavaPath { get; init; }

    public int? MemoryMb { get; init; }
}
