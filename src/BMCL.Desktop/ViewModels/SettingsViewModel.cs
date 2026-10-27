using BMCL.Core.Configuration;
using BMCL.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private const int GeneralSectionIndex = 1;
    private const int AppearanceSectionIndex = 2;
    private const int DownloadSectionIndex = 3;
    private const int GlobalSettingsSectionIndex = 6;
    private const int JavaSectionIndex = 7;
    private const int AboutSectionIndex = 10;

    private readonly LauncherSettingsService _settings;

    public SettingsViewModel(LauncherSettingsService settings, LauncherPaths paths)
    {
        _settings = settings;
        RootDirectory = paths.RootDirectory;
        ConfigFilePath = paths.ConfigFilePath;
        LoadFromSettings(settings.Settings);
        settings.Changed += (_, _) => LoadFromSettings(settings.Settings);
    }

    public string RootDirectory { get; }

    public string ConfigFilePath { get; }

    public bool IsGeneralSection => SelectedNavigationIndex == GeneralSectionIndex;

    public bool IsAppearanceSection => SelectedNavigationIndex == AppearanceSectionIndex;

    public bool IsDownloadSection => SelectedNavigationIndex == DownloadSectionIndex;

    public bool IsGlobalSettingsSection => SelectedNavigationIndex == GlobalSettingsSectionIndex;

    public bool IsJavaSection => SelectedNavigationIndex == JavaSectionIndex;

    public bool IsAboutSection => SelectedNavigationIndex == AboutSectionIndex;

    [ObservableProperty]
    private int _selectedNavigationIndex = GeneralSectionIndex;

    [ObservableProperty]
    private string? _javaPath;

    [ObservableProperty]
    private decimal? _minMemoryMb = 512;

    [ObservableProperty]
    private decimal? _maxMemoryMb = 4096;

    [ObservableProperty]
    private decimal? _maxConcurrentDownloads = 8;

    [ObservableProperty]
    private bool _useBmclApi;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedIsolationStrategyIndex = 1;

    [ObservableProperty]
    private string _status = "Settings loaded";

    partial void OnSelectedNavigationIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsGeneralSection));
        OnPropertyChanged(nameof(IsAppearanceSection));
        OnPropertyChanged(nameof(IsDownloadSection));
        OnPropertyChanged(nameof(IsGlobalSettingsSection));
        OnPropertyChanged(nameof(IsJavaSection));
        OnPropertyChanged(nameof(IsAboutSection));
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!_settings.IsLoaded)
        {
            Status = "Settings are still loading.";
            return;
        }

        var minMemory = (int)(MinMemoryMb ?? 512);
        var maxMemory = (int)(MaxMemoryMb ?? 4096);
        if (minMemory < 0 || maxMemory <= 0 || minMemory > maxMemory)
        {
            Status = "Memory values are invalid.";
            return;
        }

        var concurrency = Math.Clamp((int)(MaxConcurrentDownloads ?? 8), 1, 32);
        var updated = _settings.Settings with
        {
            JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath.Trim(),
            MinMemoryMb = minMemory,
            MaxMemoryMb = maxMemory,
            MaxConcurrentDownloads = concurrency,
            UseBmclApi = UseBmclApi,
            Theme = SelectedThemeIndex switch
            {
                1 => LauncherTheme.Light,
                2 => LauncherTheme.Dark,
                _ => LauncherTheme.System,
            },
            DefaultIsolationStrategy = SelectedIsolationStrategyIndex switch
            {
                0 => IsolationStrategy.Always,
                2 => IsolationStrategy.Never,
                _ => IsolationStrategy.Modded,
            },
        };

        try
        {
            await _settings.SaveAsync(updated, cancellationToken);
            Status = "Settings saved.";
        }
        catch (OperationCanceledException)
        {
            Status = "Saving settings was canceled.";
        }
        catch (Exception exception)
        {
            Status = $"Failed to save settings: {exception.Message}";
        }
    }

    private void LoadFromSettings(LauncherSettings settings)
    {
        JavaPath = settings.JavaPath;
        MinMemoryMb = settings.MinMemoryMb;
        MaxMemoryMb = settings.MaxMemoryMb;
        MaxConcurrentDownloads = settings.MaxConcurrentDownloads;
        UseBmclApi = settings.UseBmclApi;
        SelectedThemeIndex = settings.Theme switch
        {
            LauncherTheme.Light => 1,
            LauncherTheme.Dark => 2,
            _ => 0,
        };
        SelectedIsolationStrategyIndex = settings.DefaultIsolationStrategy switch
        {
            IsolationStrategy.Always => 0,
            IsolationStrategy.Never => 2,
            _ => 1,
        };
    }
}
