using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMCL.Auth;

public sealed record AccountStoreState
{
    public IReadOnlyList<MinecraftAccount> Accounts { get; init; } = [];

    public string? SelectedAccountId { get; init; }
}

public sealed class AccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public AccountStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public async Task<AccountStoreState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return new AccountStoreState();
        }

        await using var stream = File.OpenRead(FilePath);
        try
        {
            return await JsonSerializer.DeserializeAsync<AccountStoreState>(
                stream,
                JsonOptions,
                cancellationToken) ?? new AccountStoreState();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The account file is invalid: {FilePath}", exception);
        }
    }

    public async Task SaveAsync(
        AccountStoreState state,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("The account path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
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
