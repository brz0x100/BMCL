namespace BMCL.Install;

internal sealed record AssetIndexDocument
{
    public IReadOnlyDictionary<string, AssetObject> Objects { get; init; } =
        new Dictionary<string, AssetObject>(StringComparer.Ordinal);
}

internal sealed record AssetObject
{
    public string Hash { get; init; } = "";

    public long Size { get; init; }
}
