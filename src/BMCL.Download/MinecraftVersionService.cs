using System.Text.Json;
using BMCL.Core.Configuration;
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

    public async Task<VersionDetail> GetVersionAsync(
        string id,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ValidateVersionId(id);

        var versionDirectory = Path.Combine(_paths.Versions, id);
        var versionFile = Path.Combine(versionDirectory, $"{id}.json");

        if (!forceRefresh && await TryReadVersionAsync(versionFile, cancellationToken) is { } cached)
        {
            return cached;
        }

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

        Directory.CreateDirectory(versionDirectory);
        await WriteFileAtomicallyAsync(versionFile, json, cancellationToken);
        return detail;
    }

    private static void ValidateVersionId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            id.Contains("..", StringComparison.Ordinal) ||
            id.Contains('/', StringComparison.Ordinal) ||
            id.Contains('\\', StringComparison.Ordinal) ||
            id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("The version id is not a valid directory name.", nameof(id));
        }
    }

    private static async Task<VersionDetail?> TryReadVersionAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<VersionDetail>(
                stream,
                MinecraftJson.Options,
                cancellationToken);
        }
        catch (JsonException)
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

    private static async Task WriteFileAtomicallyAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
