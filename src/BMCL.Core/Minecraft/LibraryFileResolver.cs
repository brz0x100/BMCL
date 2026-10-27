namespace BMCL.Core.Minecraft;

public sealed record LibraryFile(string RelativePath, DownloadArtifact? Artifact);

public static class LibraryFileResolver
{
    public static LibraryFile? ResolveArtifact(MinecraftLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        var artifact = library.Downloads?.Artifact;
        var relativePath = ResolvePath(artifact?.Path, library.Name);
        return relativePath is null ? null : new LibraryFile(relativePath, artifact);
    }

    public static LibraryFile? ResolveNative(MinecraftLibrary library, string osName)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (library.Natives is null ||
            !library.Natives.TryGetValue(osName, out var classifier) ||
            string.IsNullOrWhiteSpace(classifier))
        {
            return null;
        }

        var artifact = library.Downloads?.Classifiers?.GetValueOrDefault(classifier);
        var relativePath = ResolvePath(artifact?.Path, library.Name, classifier);
        return relativePath is null ? null : new LibraryFile(relativePath, artifact);
    }

    private static string? ResolvePath(
        string? explicitPath,
        string name,
        string? classifier = null) =>
        string.IsNullOrWhiteSpace(explicitPath)
            ? MavenPath.ToRelativePath(name, classifier)
            : explicitPath;
}
