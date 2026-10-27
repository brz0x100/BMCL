using System.Text.Json;
using System.Text.Json.Serialization;
using BMCL.Core.IO;

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

    public async Task<AccountStoreState> LoadAsync(CancellationToken cancellationToken = default) =>
        await JsonFile.ReadAsync<AccountStoreState>(FilePath, JsonOptions, cancellationToken)
        ?? new AccountStoreState();

    public Task SaveAsync(
        AccountStoreState state,
        CancellationToken cancellationToken = default) =>
        JsonFile.WriteAsync(FilePath, state, JsonOptions, cancellationToken);

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
