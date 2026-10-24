using System.Text.Json;
using BMCL.Core.Configuration;

namespace BMCL.Core.Instances;

public sealed class InstanceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly LauncherPaths _paths;

    public InstanceStore(LauncherPaths paths)
    {
        _paths = paths;
    }

    public async Task<IReadOnlyList<Instance>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var root = _paths.Versions;
        if (!Directory.Exists(root))
        {
            return [];
        }

        var instances = new List<Instance>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetFileName(directory);
            var metadataPath = Path.Combine(directory, "instance.json");
            var instance = await TryReadInstanceAsync(metadataPath, cancellationToken);

            if (instance is null)
            {
                var versionFile = Path.Combine(directory, $"{id}.json");
                if (!File.Exists(versionFile))
                {
                    continue;
                }

                instance = new Instance
                {
                    Id = id,
                    Name = id,
                    VersionId = id,
                    Isolated = HasLocalGameData(directory),
                };
            }

            instances.Add(instance with
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(instance.Name) ? id : instance.Name,
                VersionId = string.IsNullOrWhiteSpace(instance.VersionId) ? id : instance.VersionId,
            });
        }

        return instances
            .OrderByDescending(instance => instance.LastPlayedAt ?? instance.CreatedAt)
            .ToList();
    }

    public async Task SaveAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        ValidateId(instance.Id);

        var directory = Path.Combine(_paths.Versions, instance.Id);
        Directory.CreateDirectory(directory);

        var metadataPath = Path.Combine(directory, "instance.json");
        var temporaryPath = $"{metadataPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, instance, JsonOptions, cancellationToken);
            }

            File.Move(temporaryPath, metadataPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task<Instance?> TryReadInstanceAsync(
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
            return await JsonSerializer.DeserializeAsync<Instance>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasLocalGameData(string directory) =>
        File.Exists(Path.Combine(directory, "options.txt")) ||
        Directory.Exists(Path.Combine(directory, "saves")) ||
        Directory.Exists(Path.Combine(directory, "mods")) ||
        Directory.Exists(Path.Combine(directory, "config")) ||
        Directory.Exists(Path.Combine(directory, "resourcepacks")) ||
        Directory.Exists(Path.Combine(directory, "shaderpacks"));

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            id.Contains("..", StringComparison.Ordinal) ||
            id.Contains('/', StringComparison.Ordinal) ||
            id.Contains('\\', StringComparison.Ordinal) ||
            id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("The instance id is not a valid directory name.", nameof(id));
        }
    }
}
