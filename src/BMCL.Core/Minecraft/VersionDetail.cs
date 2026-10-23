namespace BMCL.Core.Minecraft;

public sealed record VersionDetail
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "";

    public string MainClass { get; init; } = "";

    public string? InheritsFrom { get; init; }

    public string? Assets { get; init; }

    public AssetIndex? AssetIndex { get; init; }

    public VersionDownloads? Downloads { get; init; }

    public IReadOnlyList<MinecraftLibrary> Libraries { get; init; } = [];

    public VersionArguments? Arguments { get; init; }

    public string? MinecraftArguments { get; init; }

    public JavaVersion? JavaVersion { get; init; }

    public LoggingConfiguration? Logging { get; init; }

    public int? ComplianceLevel { get; init; }
}

public sealed record AssetIndex
{
    public string Id { get; init; } = "";

    public string? Sha1 { get; init; }

    public long? Size { get; init; }

    public long? TotalSize { get; init; }

    public string Url { get; init; } = "";
}

public sealed record VersionDownloads
{
    public DownloadArtifact? Client { get; init; }
}

public sealed record DownloadArtifact
{
    public string? Path { get; init; }

    public string? Sha1 { get; init; }

    public long? Size { get; init; }

    public string Url { get; init; } = "";
}

public sealed record MinecraftLibrary
{
    public string Name { get; init; } = "";

    public LibraryDownloads? Downloads { get; init; }

    public IReadOnlyDictionary<string, string>? Natives { get; init; }

    public IReadOnlyList<Rule>? Rules { get; init; }

    public LibraryExtract? Extract { get; init; }
}

public sealed record LibraryDownloads
{
    public DownloadArtifact? Artifact { get; init; }

    public IReadOnlyDictionary<string, DownloadArtifact>? Classifiers { get; init; }
}

public sealed record LibraryExtract
{
    public IReadOnlyList<string>? Exclude { get; init; }
}

public sealed record JavaVersion
{
    public string Component { get; init; } = "";

    public int MajorVersion { get; init; }
}

public sealed record LoggingConfiguration
{
    public LoggingClient? Client { get; init; }
}

public sealed record LoggingClient
{
    public string? Argument { get; init; }

    public LoggingFile? File { get; init; }
}

public sealed record LoggingFile
{
    public string Id { get; init; } = "";

    public string? Sha1 { get; init; }

    public long? Size { get; init; }

    public string Url { get; init; } = "";
}
