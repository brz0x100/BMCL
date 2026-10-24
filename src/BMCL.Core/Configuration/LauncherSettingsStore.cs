using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMCL.Core.Configuration;

public sealed class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public LauncherSettingsStore(LauncherPaths paths)
    {
        FilePath = paths.ConfigFile;
        LegacyFilePath = paths.Root == paths.ConfigRoot
            ? null
            : Path.Combine(paths.Root, "config.json");
    }

    public string FilePath { get; }

    public string? LegacyFilePath { get; }

    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var sourcePath = File.Exists(FilePath) ? FilePath : LegacyFilePath;
        if (sourcePath is null || !File.Exists(sourcePath))
        {
            return new LauncherSettings();
        }

        await using var stream = File.OpenRead(sourcePath);
        try
        {
            return await JsonSerializer.DeserializeAsync<LauncherSettings>(
                stream,
                JsonOptions,
                cancellationToken) ?? new LauncherSettings();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The launcher configuration is invalid: {sourcePath}", exception);
        }
    }

    public async Task SaveAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("The configuration path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
