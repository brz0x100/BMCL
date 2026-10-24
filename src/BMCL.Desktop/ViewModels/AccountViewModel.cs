using System.Collections.ObjectModel;
using BMCL.Auth;
using BMCL.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class AccountViewModel : ViewModelBase
{
    private readonly AccountManager _accounts;

    public AccountViewModel(AccountManager accounts)
    {
        _accounts = accounts;
        SelectedAccount = accounts.SelectedAccount;
        Accounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAccounts));
        accounts.Changed += (_, _) =>
        {
            if (SelectedAccount is null)
            {
                SelectedAccount = accounts.SelectedAccount;
            }

            OnPropertyChanged(nameof(HasAccounts));
        };
    }

    public ObservableCollection<MinecraftAccount> Accounts => _accounts.Accounts;

    public bool HasAccounts => Accounts.Count > 0;

    [ObservableProperty]
    private MinecraftAccount? _selectedAccount;

    [ObservableProperty]
    private string _newAccountName = "";

    [ObservableProperty]
    private string _status = "Create an offline account to get started.";

    [RelayCommand]
    private async Task AddOfflineAccountAsync(CancellationToken cancellationToken)
    {
        var name = NewAccountName.Trim();
        if (name.Length == 0)
        {
            Status = "Enter a player name.";
            return;
        }

        try
        {
            await _accounts.AddOfflineAccountAsync(name, cancellationToken);
            NewAccountName = "";
            SelectedAccount = _accounts.SelectedAccount;
            Status = "Offline account added and selected.";
        }
        catch (Exception exception)
        {
            Status = exception.Message;
        }
    }

    [RelayCommand]
    private async Task UseSelectedAccountAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            Status = "Select an account first.";
            return;
        }

        try
        {
            await _accounts.SelectAccountAsync(SelectedAccount, cancellationToken);
            Status = $"Using {SelectedAccount.Name}.";
        }
        catch (Exception exception)
        {
            Status = exception.Message;
        }
    }
}
