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
        var root = _paths.Instances;
        if (!Directory.Exists(root))
        {
            return [];
        }

        var instances = new List<Instance>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadataPath = Path.Combine(directory, "instance.json");
            if (!File.Exists(metadataPath))
            {
                continue;
            }

            try
            {
                await using var stream = File.OpenRead(metadataPath);
                var instance = await JsonSerializer.DeserializeAsync<Instance>(
                    stream,
                    JsonOptions,
                    cancellationToken);
                if (instance is not null)
                {
                    instances.Add(instance);
                }
            }
            catch (JsonException)
            {
                // Ignore incomplete or corrupted instance metadata.
            }
        }

        return instances
            .OrderByDescending(instance => instance.LastPlayedAt ?? instance.CreatedAt)
            .ToList();
    }

    public async Task SaveAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        ValidateId(instance.Id);

        var directory = Path.Combine(_paths.Instances, instance.Id);
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
