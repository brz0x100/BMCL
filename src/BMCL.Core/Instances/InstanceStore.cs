using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.IO;

namespace BMCL.Core.Instances;

public sealed class InstanceStore
{
    public const string InstallationMarkerFileName = ".installing";

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
        var root = _paths.VersionsDirectory;
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
                var hasLocalData = HasLocalGameData(directory);
                if (!File.Exists(versionFile) ||
                    File.Exists(Path.Combine(directory, InstallationMarkerFileName)) ||
                    (!hasLocalData && !File.Exists(Path.Combine(directory, $"{id}.jar"))))
                {
                    continue;
                }

                instance = new Instance
                {
                    Id = id,
                    Name = id,
                    VersionId = id,
                    Isolated = hasLocalData,
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
        PathSafety.ValidateFileName(instance.Id, nameof(instance), "instance id");

        var directory = Path.Combine(_paths.VersionsDirectory, instance.Id);
        var metadataPath = Path.Combine(directory, "instance.json");
        await JsonFile.WriteAsync(metadataPath, instance, JsonOptions, cancellationToken);
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
            return await JsonFile.ReadAsync<Instance>(path, JsonOptions, cancellationToken);
        }
        catch (InvalidDataException)
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
}
