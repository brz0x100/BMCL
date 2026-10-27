using System.Text.Json;
using System.Text.Json.Serialization;
using BMCL.Core.IO;

namespace BMCL.Core.Configuration;

public sealed class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public LauncherSettingsStore(LauncherPaths paths)
    {
        FilePath = paths.ConfigFilePath;
        LegacyFilePath = paths.RootDirectory == paths.ConfigDirectory
            ? null
            : Path.Combine(paths.RootDirectory, "config.json");
    }

    public string FilePath { get; }

    public string? LegacyFilePath { get; }

    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var sourcePath = File.Exists(FilePath) ? FilePath : LegacyFilePath;
        if (sourcePath is null)
        {
            return new LauncherSettings();
        }

        return await JsonFile.ReadAsync<LauncherSettings>(sourcePath, JsonOptions, cancellationToken)
               ?? new LauncherSettings();
    }

    public Task SaveAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken = default) =>
        JsonFile.WriteAsync(FilePath, settings, JsonOptions, cancellationToken);

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
