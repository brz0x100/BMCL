using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.IO;
using BMCL.Core.Minecraft;

namespace BMCL.Download;

public sealed class MinecraftVersionService
{
    private readonly HttpClient _httpClient;
    private readonly LauncherPaths _paths;
    private readonly IMirrorUrlMapper _mirror;
    private readonly SemaphoreSlim _manifestLock = new(1, 1);
    private VersionManifest? _manifest;

    public MinecraftVersionService(
        HttpClient httpClient,
        LauncherPaths paths,
        IMirrorUrlMapper mirror)
    {
        _httpClient = httpClient;
        _paths = paths;
        _mirror = mirror;
    }

    public async Task<VersionManifest> GetManifestAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _manifest is not null)
            {
                return _manifest;
            }

            var json = await GetStringAsync(MinecraftEndpoints.VersionManifest, cancellationToken);
            _manifest = JsonSerializer.Deserialize<VersionManifest>(json, MinecraftJson.Options)
                ?? throw new InvalidDataException("The version manifest is empty.");
            return _manifest;
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    public Task<VersionDetail> GetVersionAsync(
        string id,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default) =>
        GetVersionAsync(
            id,
            forceRefresh,
            new HashSet<string>(StringComparer.Ordinal),
            cancellationToken);

    private async Task<VersionDetail> GetVersionAsync(
        string id,
        bool forceRefresh,
        HashSet<string> resolvingVersions,
        CancellationToken cancellationToken)
    {
        PathSafety.ValidateFileName(id, nameof(id), "version id");

        if (!resolvingVersions.Add(id))
        {
            throw new InvalidDataException($"The version inheritance chain contains a cycle at '{id}'.");
        }

        try
        {
            var versionDirectory = Path.Combine(_paths.VersionsDirectory, id);
            var versionFile = Path.Combine(versionDirectory, $"{id}.json");

            var version = !forceRefresh &&
                          await TryReadVersionAsync(versionFile, cancellationToken) is { } cached
                ? cached
                : await DownloadVersionAsync(id, versionFile, cancellationToken);

            if (string.IsNullOrWhiteSpace(version.InheritsFrom))
            {
                return version;
            }

            var parent = await GetVersionAsync(
                version.InheritsFrom,
                forceRefresh,
                resolvingVersions,
                cancellationToken);
            return VersionDetailMerger.Merge(parent, version);
        }
        finally
        {
            resolvingVersions.Remove(id);
        }
    }

    private async Task<VersionDetail> DownloadVersionAsync(
        string id,
        string versionFile,
        CancellationToken cancellationToken)
    {
        var manifest = await GetManifestAsync(false, cancellationToken);
        var summary = manifest.Versions.FirstOrDefault(
            version => string.Equals(version.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Version '{id}' was not found in the manifest.");

        if (!Uri.TryCreate(summary.Url, UriKind.Absolute, out var url))
        {
            throw new InvalidDataException($"Version '{id}' has an invalid URL.");
        }

        var json = await GetStringAsync(url, cancellationToken);
        var detail = JsonSerializer.Deserialize<VersionDetail>(json, MinecraftJson.Options)
            ?? throw new InvalidDataException($"Version '{id}' could not be parsed.");

        await AtomicFile.WriteAllTextAsync(versionFile, json, cancellationToken);
        return detail;
    }

    private static async Task<VersionDetail?> TryReadVersionAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            return await JsonFile.ReadAsync<VersionDetail>(
                path,
                MinecraftJson.Options,
                cancellationToken);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            _mirror.Map(uri),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
