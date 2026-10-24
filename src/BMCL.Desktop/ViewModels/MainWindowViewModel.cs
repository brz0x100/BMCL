using Avalonia;
using Avalonia.Styling;
using BMCL.Auth;
using BMCL.Core.Configuration;
using BMCL.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly LauncherSettingsService _settings;
    private readonly AccountManager _accounts;
    private readonly GameLaunchService _gameLauncher;

    public MainWindowViewModel(
        LauncherSettingsService settings,
        AccountManager accounts,
        GameLaunchService gameLauncher,
        HomeViewModel homePage,
        InstancesViewModel instancesPage,
        DownloadsViewModel downloadsPage,
        SettingsViewModel settingsPage,
        AccountViewModel accountPage)
    {
        _settings = settings;
        _accounts = accounts;
        _gameLauncher = gameLauncher;

        HomePage = homePage;
        InstancesPage = instancesPage;
        DownloadsPage = downloadsPage;
        SettingsPage = settingsPage;
        AccountPage = accountPage;

        _currentPage = homePage;
        _selectedPageIndex = 0;

        _accounts.Changed += (_, _) =>
        {
            UpdateAccountState();
            NotifyLaunchState();
        };
        _settings.Changed += (_, _) => ApplyTheme(_settings.Settings.Theme);
        InstancesPage.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(InstancesViewModel.SelectedInstance))
            {
                NotifyLaunchState();
            }
        };

        UpdateAccountState();
        ApplyTheme(_settings.Settings.Theme);
    }

    public HomeViewModel HomePage { get; }

    public InstancesViewModel InstancesPage { get; }

    public DownloadsViewModel DownloadsPage { get; }

    public SettingsViewModel SettingsPage { get; }

    public AccountViewModel AccountPage { get; }

    public string AccountName => _accounts.SelectedAccount?.Name ?? "Not signed in";

    public string AccountTypeText => _accounts.SelectedAccount?.Type == AccountType.Microsoft
        ? "Microsoft"
        : "Offline";

    public bool CanLaunch =>
        !IsLaunching &&
        _accounts.SelectedAccount is not null &&
        InstancesPage.SelectedInstance is not null;

    public string LaunchButtonText => IsLaunching ? "Launching..." : "Launch Game";

    public bool IsHomePage => CurrentPage == HomePage;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private int _selectedPageIndex;

    [ObservableProperty]
    private string _status = "Starting...";

    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isLaunching;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Status = "Loading launcher data...";
        var warnings = new List<string>();

        try
        {
            await _settings.LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            warnings.Add($"Settings: {exception.Message}");
        }

        try
        {
            await _accounts.LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            warnings.Add($"Accounts: {exception.Message}");
        }

        await InstancesPage.LoadAsync(cancellationToken);
        await DownloadsPage.LoadAsync(cancellationToken: cancellationToken);

        Status = warnings.Count == 0 ? "Ready" : string.Join(" | ", warnings);
    }

    [RelayCommand]
    private void OpenInstances()
    {
        SelectedPageIndex = 1;
        CurrentPage = InstancesPage;
    }

    [RelayCommand]
    private async Task LaunchGameAsync(CancellationToken cancellationToken)
    {
        if (!CanLaunch || InstancesPage.SelectedInstance is not { } instance)
        {
            return;
        }

        IsLaunching = true;
        Status = $"Launching {instance.Name}...";

        try
        {
            var result = await _gameLauncher.LaunchAsync(instance, cancellationToken);
            Status = $"Launched {instance.Name} with {Path.GetFileName(result.JavaPath)}.";
        }
        catch (OperationCanceledException)
        {
            Status = "Launch canceled.";
        }
        catch (Exception exception)
        {
            Status = $"Launch failed: {exception.Message}";
        }
        finally
        {
            IsLaunching = false;
        }
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsHomePage));
    }

    partial void OnSelectedPageIndexChanged(int value)
    {
        CurrentPage = value switch
        {
            0 => HomePage,
            1 => InstancesPage,
            2 => DownloadsPage,
            _ => CurrentPage,
        };
    }

    partial void OnIsLaunchingChanged(bool value) => NotifyLaunchState();

    private void UpdateAccountState()
    {
        OnPropertyChanged(nameof(AccountName));
        OnPropertyChanged(nameof(AccountTypeText));
    }

    private void NotifyLaunchState()
    {
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(LaunchButtonText));
    }

    private void ApplyTheme(LauncherTheme theme)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        application.RequestedThemeVariant = theme switch
        {
            LauncherTheme.Light => ThemeVariant.Light,
            LauncherTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
