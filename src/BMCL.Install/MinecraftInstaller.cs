using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using BMCL.Core.Minecraft;
using BMCL.Download;
using BMCL.Launch;

namespace BMCL.Install;

public sealed class MinecraftInstaller
{
    private static readonly JsonSerializerOptions AssetIndexJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MinecraftVersionService _versions;
    private readonly IDownloadService _downloads;
    private readonly LauncherPaths _paths;
    private readonly InstanceStore _instances;

    public MinecraftInstaller(
        MinecraftVersionService versions,
        IDownloadService downloads,
        LauncherPaths paths,
        InstanceStore instances)
    {
        _versions = versions;
        _downloads = downloads;
        _paths = paths;
        _instances = instances;
    }

    public Task<Instance> InstallAsync(
        string versionId,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        InstallAsync(
            versionId,
            instanceName: null,
            IsolationStrategy.Always,
            progress,
            cancellationToken);

    public async Task<Instance> InstallAsync(
        string versionId,
        string? instanceName,
        IsolationStrategy isolationStrategy,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);

        progress?.Report(new InstallProgress("Resolving version"));
        var version = await _versions.GetVersionAsync(versionId, cancellationToken: cancellationToken);
        var context = RuleContext.Current;
        var isolated = ShouldIsolate(isolationStrategy, version, versionId);

        Directory.CreateDirectory(_paths.Versions);
        Directory.CreateDirectory(_paths.Libraries);
        Directory.CreateDirectory(_paths.Assets);

        var downloads = CreateDownloadPlan(version, versionId, out var assetIndexPath, out var loggingPath);

        await DownloadAsync(downloads, "Downloading game files", progress, cancellationToken);

        await DownloadAssetsAsync(
            version,
            assetIndexPath,
            progress,
            cancellationToken);

        if (loggingPath is not null && !File.Exists(loggingPath))
        {
            throw new IOException("The logging configuration was not downloaded.");
        }

        var instanceDirectory = isolated
            ? Path.Combine(_paths.Versions, versionId)
            : _paths.MinecraftRoot;
        Directory.CreateDirectory(instanceDirectory);
        var nativesDirectory = Path.Combine(instanceDirectory, "natives");
        progress?.Report(new InstallProgress("Extracting native libraries"));
        NativesExtractor.Extract(
            version,
            context,
            _paths.Libraries,
            nativesDirectory);

        var instance = await CreateInstanceAsync(
            versionId,
            instanceName,
            isolated,
            cancellationToken);
        progress?.Report(new InstallProgress("Installation complete", 1, 1));
        return instance;
    }

    private List<DownloadItem> CreateDownloadPlan(
        VersionDetail version,
        string versionId,
        out string? assetIndexPath,
        out string? loggingPath)
    {
        var result = new Dictionary<string, DownloadItem>(StringComparer.Ordinal);
        assetIndexPath = null;
        loggingPath = null;

        AddArtifact(
            result,
            version.Downloads?.Client,
            Path.Combine(_paths.Versions, versionId, $"{versionId}.jar"));

        var context = RuleContext.Current;
        foreach (var library in version.Libraries)
        {
            if (!RuleEvaluator.IsAllowed(library.Rules, context))
            {
                continue;
            }

            var artifact = library.Downloads?.Artifact;
            if (artifact is not null)
            {
                var relativePath = artifact.Path ?? MavenPath.ToRelativePath(library.Name);
                if (relativePath is not null)
                {
                    AddArtifact(
                        result,
                        artifact,
                        Path.Combine(
                            _paths.Libraries,
                            relativePath.Replace('/', Path.DirectorySeparatorChar)));
                }
            }

            if (library.Natives is null ||
                !library.Natives.TryGetValue(context.OsName, out var classifier))
            {
                continue;
            }

            var nativeArtifact = library.Downloads?.Classifiers?.GetValueOrDefault(classifier);
            var nativeRelativePath = nativeArtifact?.Path ?? MavenPath.ToRelativePath(library.Name, classifier);
            if (nativeArtifact is not null && nativeRelativePath is not null)
            {
                AddArtifact(
                    result,
                    nativeArtifact,
                    Path.Combine(
                        _paths.Libraries,
                        nativeRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            }
        }

        if (version.AssetIndex is { } assetIndex &&
            !string.IsNullOrWhiteSpace(assetIndex.Url))
        {
            var id = string.IsNullOrWhiteSpace(assetIndex.Id)
                ? version.Assets ?? "legacy"
                : assetIndex.Id;
            assetIndexPath = Path.Combine(_paths.Assets, "indexes", $"{id}.json");
            AddDownload(
                result,
                new DownloadItem(
                    CreateUri(assetIndex.Url),
                    assetIndexPath,
                    assetIndex.Sha1,
                    assetIndex.Size));
        }

        if (version.Logging?.Client?.File is { } loggingFile &&
            !string.IsNullOrWhiteSpace(loggingFile.Url))
        {
            loggingPath = Path.Combine(_paths.Assets, "log_configs", loggingFile.Id);
            AddDownload(
                result,
                new DownloadItem(
                    CreateUri(loggingFile.Url),
                    loggingPath,
                    loggingFile.Sha1,
                    loggingFile.Size));
        }

        return result.Values.ToList();
    }

    private async Task DownloadAssetsAsync(
        VersionDetail version,
        string? assetIndexPath,
        IProgress<InstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (assetIndexPath is null || !File.Exists(assetIndexPath))
        {
            return;
        }

        AssetIndexDocument? index;
        await using (var stream = File.OpenRead(assetIndexPath))
        {
            index = await JsonSerializer.DeserializeAsync<AssetIndexDocument>(
                stream,
                AssetIndexJsonOptions,
                cancellationToken);
        }

        if (index is null)
        {
            throw new InvalidDataException($"The asset index for '{version.Id}' is empty.");
        }

        var items = new Dictionary<string, DownloadItem>(StringComparer.Ordinal);
        foreach (var (_, asset) in index.Objects)
        {
            if (asset.Hash.Length < 2)
            {
                continue;
            }

            var prefix = asset.Hash[..2];
            var destination = Path.Combine(_paths.Assets, "objects", prefix, asset.Hash);
            if (items.ContainsKey(destination))
            {
                continue;
            }

            var url = new Uri(MinecraftEndpoints.AssetBase, $"{prefix}/{asset.Hash}");
            items[destination] = new DownloadItem(url, destination, asset.Hash, asset.Size);
        }

        await DownloadAsync(items.Values.ToList(), "Downloading assets", progress, cancellationToken);
    }

    private async Task<Instance> CreateInstanceAsync(
        string versionId,
        string? instanceName,
        bool isolated,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(instanceName)
            ? versionId
            : instanceName.Trim();
        var existing = (await _instances.LoadAsync(cancellationToken))
            .FirstOrDefault(instance => string.Equals(instance.Id, versionId, StringComparison.Ordinal));

        var instance = existing is null
            ? new Instance
            {
                Id = versionId,
                Name = name,
                VersionId = versionId,
            }
            : existing with
            {
                Name = name,
                VersionId = versionId,
                Isolated = isolated,
            };

        Directory.CreateDirectory(Path.Combine(_paths.Versions, instance.Id));
        await _instances.SaveAsync(instance, cancellationToken);
        return instance;
    }

    private static bool ShouldIsolate(
        IsolationStrategy strategy,
        VersionDetail version,
        string versionId) => strategy switch
    {
        IsolationStrategy.Always => true,
        IsolationStrategy.Never => false,
        _ => IsModdedVersion(version, versionId),
    };

    private static bool IsModdedVersion(VersionDetail version, string versionId)
    {
        if (!string.IsNullOrWhiteSpace(version.InheritsFrom))
        {
            return true;
        }

        return ContainsLoader(versionId) ||
               version.Libraries.Any(library => ContainsLoader(library.Name));
    }

    private static bool ContainsLoader(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower.Contains("forge", StringComparison.Ordinal) ||
               lower.Contains("fabric", StringComparison.Ordinal) ||
               lower.Contains("quilt", StringComparison.Ordinal) ||
               lower.Contains("neoforge", StringComparison.Ordinal) ||
               lower.Contains("optifine", StringComparison.Ordinal) ||
               lower.Contains("liteloader", StringComparison.Ordinal) ||
               lower.Contains("cleanroom", StringComparison.Ordinal);
    }

    private async Task DownloadAsync(
        IReadOnlyList<DownloadItem> items,
        string stage,
        IProgress<InstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var downloadProgress = progress is null
            ? null
            : new Progress<DownloadProgress>(
                update => progress.Report(new InstallProgress(
                    stage,
                    update.Completed,
                    update.Total,
                    update.FileName)));

        await _downloads.DownloadAsync(items, downloadProgress, cancellationToken);
    }

    private static void AddArtifact(
        IDictionary<string, DownloadItem> items,
        DownloadArtifact? artifact,
        string destination)
    {
        if (artifact is null || string.IsNullOrWhiteSpace(artifact.Url))
        {
            return;
        }

        AddDownload(
            items,
            new DownloadItem(
                CreateUri(artifact.Url),
                destination,
                artifact.Sha1,
                artifact.Size));
    }

    private static void AddDownload(
        IDictionary<string, DownloadItem> items,
        DownloadItem item)
    {
        items[item.Destination] = item;
    }

    private static Uri CreateUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidDataException($"The download URL is invalid: {value}");
        }

        return uri;
    }
}
