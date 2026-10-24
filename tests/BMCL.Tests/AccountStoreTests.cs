using BMCL.Auth;
using Xunit;

namespace BMCL.Tests;

public sealed class AccountStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "bmcl-account-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadRoundTripsAccounts()
    {
        var account = OfflineAccountFactory.Create("Steve");
        var store = new AccountStore(Path.Combine(_root, "accounts.json"));
        var state = new AccountStoreState
        {
            Accounts = [account],
            SelectedAccountId = account.Uuid,
        };

        await store.SaveAsync(state);
        var loaded = await store.LoadAsync();

        Assert.Equal(account, Assert.Single(loaded.Accounts));
        Assert.Equal(account.Uuid, loaded.SelectedAccountId);
    }

    [Fact]
    public async Task LoadReturnsEmptyStateWhenFileDoesNotExist()
    {
        var store = new AccountStore(Path.Combine(_root, "accounts.json"));

        var loaded = await store.LoadAsync();

        Assert.Empty(loaded.Accounts);
        Assert.Null(loaded.SelectedAccountId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
