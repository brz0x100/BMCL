using System.Collections.ObjectModel;
using BMCL.Auth;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BMCL.Desktop.Services;

public sealed partial class AccountManager : ObservableObject
{
    private readonly AccountStore _store;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public AccountManager(AccountStore store)
    {
        _store = store;
    }

    public ObservableCollection<MinecraftAccount> Accounts { get; } = new();

    public event EventHandler? Changed;

    [ObservableProperty]
    private MinecraftAccount? _selectedAccount;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            var state = await _store.LoadAsync(cancellationToken);

            Accounts.Clear();
            foreach (var account in state.Accounts)
            {
                Accounts.Add(account);
            }

            SelectedAccount = Accounts.FirstOrDefault(
                               account => string.Equals(
                                   account.Uuid,
                                   state.SelectedAccountId,
                                   StringComparison.OrdinalIgnoreCase))
                           ?? Accounts.FirstOrDefault();
        }
        finally
        {
            _saveLock.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddOfflineAccountAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length is < 3 or > 16)
        {
            throw new ArgumentException("Offline player names must be between 3 and 16 characters.");
        }

        var account = OfflineAccountFactory.Create(trimmedName);

        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (Accounts.Any(existing =>
                    string.Equals(existing.Name, account.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(existing.Uuid, account.Uuid, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("An offline account with that name already exists.");
            }

            var previous = SelectedAccount;
            Accounts.Add(account);
            SelectedAccount = account;
            try
            {
                await SaveCoreAsync(cancellationToken);
            }
            catch
            {
                Accounts.Remove(account);
                SelectedAccount = previous;
                throw;
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public async Task SelectAccountAsync(
        MinecraftAccount? account,
        CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (account is not null && !Accounts.Contains(account))
            {
                throw new ArgumentException(
                    "The selected account is not part of the account list.",
                    nameof(account));
            }

            var previous = SelectedAccount;
            SelectedAccount = account;
            try
            {
                await SaveCoreAsync(cancellationToken);
            }
            catch
            {
                SelectedAccount = previous;
                throw;
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task SaveCoreAsync(CancellationToken cancellationToken)
    {
        await _store.SaveAsync(
            new AccountStoreState
            {
                Accounts = Accounts.ToList(),
                SelectedAccountId = SelectedAccount?.Uuid,
            },
            cancellationToken);

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
