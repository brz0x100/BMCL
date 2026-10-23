namespace BMCL.Auth;

public enum AccountType
{
    Offline,
    Microsoft,
}

public sealed record MinecraftAccount
{
    public required string Name { get; init; }

    public required string Uuid { get; init; }

    public AccountType Type { get; init; } = AccountType.Offline;

    public string? AccessToken { get; init; }
}
