using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using BMCL.Core.IO;
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
        PathSafety.ValidateFileName(versionId, nameof(versionId), "version id");

        var versionDirectory = Path.Combine(_paths.VersionsDirectory, versionId);
        var markerPath = Path.Combine(versionDirectory, InstanceStore.InstallationMarkerFileName);
        Directory.CreateDirectory(versionDirectory);
        await File.WriteAllTextAsync(markerPath, string.Empty, cancellationToken);

        var completed = false;
        try
        {
            progress?.Report(new InstallProgress("Resolving version"));
            var version = await _versions.GetVersionAsync(versionId, cancellationToken: cancellationToken);
            var context = RuleContext.Current;
            var isolated = ShouldIsolate(isolationStrategy, version, versionId);

            Directory.CreateDirectory(_paths.LibrariesDirectory);
            Directory.CreateDirectory(_paths.AssetsDirectory);

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

            var instanceDirectory = isolated ? versionDirectory : _paths.MinecraftDirectory;
            Directory.CreateDirectory(instanceDirectory);
            var nativesDirectory = Path.Combine(instanceDirectory, "natives");
            progress?.Report(new InstallProgress("Extracting native libraries"));
            NativesExtractor.Extract(
                version,
                context,
                _paths.LibrariesDirectory,
                nativesDirectory);

            var instance = await CreateInstanceAsync(
                versionId,
                instanceName,
                isolated,
                cancellationToken);
            progress?.Report(new InstallProgress("Installation complete", 1, 1));
            completed = true;
            return instance;
        }
        finally
        {
            if (completed)
            {
                TryDelete(markerPath);
            }
        }
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
            Path.Combine(_paths.VersionsDirectory, versionId, $"{versionId}.jar"));

        var context = RuleContext.Current;
        foreach (var library in version.Libraries)
        {
            if (!RuleEvaluator.IsAllowed(library.Rules, context))
            {
                continue;
            }

            if (LibraryFileResolver.ResolveArtifact(library) is { } artifactFile)
            {
                AddLibraryFile(result, artifactFile);
            }

            if (LibraryFileResolver.ResolveNative(library, context.OsName) is { } nativeFile)
            {
                AddLibraryFile(result, nativeFile);
            }
        }

        if (version.AssetIndex is { } assetIndex &&
            !string.IsNullOrWhiteSpace(assetIndex.Url))
        {
            var id = string.IsNullOrWhiteSpace(assetIndex.Id)
                ? version.Assets ?? "legacy"
                : assetIndex.Id;
            PathSafety.ValidateFileName(id, nameof(assetIndex), "asset index id");
            assetIndexPath = Path.Combine(_paths.AssetIndexesDirectory, $"{id}.json");
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
            PathSafety.ValidateFileName(loggingFile.Id, nameof(loggingFile), "logging configuration id");
            loggingPath = Path.Combine(_paths.AssetsDirectory, "log_configs", loggingFile.Id);
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

        var index = await JsonFile.ReadAsync<AssetIndexDocument>(
            assetIndexPath,
            AssetIndexJsonOptions,
            cancellationToken);

        if (index is null)
        {
            throw new InvalidDataException($"The asset index for '{version.Id}' is empty.");
        }

        var items = new Dictionary<string, DownloadItem>(StringComparer.Ordinal);
        foreach (var (_, asset) in index.Objects)
        {
            if (asset.Hash.Length != 40 || !asset.Hash.All(char.IsAsciiHexDigit))
            {
                continue;
            }

            var prefix = asset.Hash[..2];
            var destination = Path.Combine(_paths.AssetObjectsDirectory, prefix, asset.Hash);
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
        var existing = (await _instances.LoadAsync(cancellationToken))
            .FirstOrDefault(instance => string.Equals(instance.Id, versionId, StringComparison.Ordinal));
        var name = string.IsNullOrWhiteSpace(instanceName)
            ? existing?.Name ?? versionId
            : instanceName.Trim();

        var instance = existing is null
            ? new Instance
            {
                Id = versionId,
                Name = name,
                VersionId = versionId,
                Isolated = isolated,
            }
            : existing with
            {
                Name = name,
                VersionId = versionId,
                Isolated = isolated,
            };

        Directory.CreateDirectory(Path.Combine(_paths.VersionsDirectory, instance.Id));
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

    private void AddLibraryFile(
        IDictionary<string, DownloadItem> items,
        LibraryFile file)
    {
        var destination = PathSafety.CombineUnderRoot(
            _paths.LibrariesDirectory,
            file.RelativePath);
        var artifact = file.Artifact;
        var url = artifact is { } download && !string.IsNullOrWhiteSpace(download.Url)
            ? CreateUri(download.Url)
            : new Uri(MinecraftEndpoints.LibraryBase, file.RelativePath);

        AddDownload(
            items,
            new DownloadItem(
                url,
                destination,
                artifact?.Sha1,
                artifact?.Size));
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

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
