using System.Collections.ObjectModel;
using System.Net.Http;
using BMCL.Core.Minecraft;
using BMCL.Download;
using BMCL.Install;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class DownloadsViewModel : ViewModelBase
{
    private readonly MinecraftVersionService _versionService;
    private readonly MinecraftInstaller _installer;
    private readonly InstancesViewModel _instances;
    private readonly List<VersionSummary> _allVersions = [];

    public DownloadsViewModel(
        MinecraftVersionService versionService,
        MinecraftInstaller installer,
        InstancesViewModel instances)
    {
        _versionService = versionService;
        _installer = installer;
        _instances = instances;
    }

    public ObservableCollection<VersionSummary> Versions { get; } = new();

    [ObservableProperty]
    private VersionSummary? _selectedVersion;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private double _installProgress;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _status = "Version list not loaded";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _selectedSectionIndex;

    [ObservableProperty]
    private int _selectedNewGameTabIndex;

    [ObservableProperty]
    private int _selectedAddonsTabIndex;

    private static readonly string[] NewGameTabs = ["Games", "Modpacks"];
    private static readonly string[] AddonTabs = ["Mods", "Resource Packs", "Shaders", "Worlds"];

    public bool IsNewGameSection => SelectedSectionIndex != 1;

    public bool IsAddonsSection => SelectedSectionIndex == 1;

    public bool IsGamesSelected => IsNewGameSection && SelectedNewGameTabIndex != 1;

    public string SelectedTabLabel => IsNewGameSection
        ? NewGameTabs[Math.Clamp(SelectedNewGameTabIndex, 0, NewGameTabs.Length - 1)]
        : AddonTabs[Math.Clamp(SelectedAddonsTabIndex, 0, AddonTabs.Length - 1)];

    public string PageStatus => IsGamesSelected
        ? Status
        : $"{SelectedTabLabel} browsing will be added in a later milestone.";

    public bool CanInstall => !IsInstalling && SelectedVersion is not null && IsGamesSelected;

    public async Task LoadAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Status = "Loading version list...";

        try
        {
            var manifest = await _versionService.GetManifestAsync(forceRefresh, cancellationToken);

            _allVersions.Clear();
            _allVersions.AddRange(manifest.Versions.OrderByDescending(version => version.ReleaseTime));
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            Status = "Canceled";
        }
        catch (HttpRequestException exception)
        {
            Status = $"Network request failed: {exception.Message}";
        }
        catch (Exception exception)
        {
            Status = $"Failed to load versions: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) =>
        LoadAsync(forceRefresh: true, cancellationToken);

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(PageStatus));

    partial void OnSelectedSectionIndexChanged(int value) => NotifyTabState();

    partial void OnSelectedNewGameTabIndexChanged(int value) => NotifyTabState();

    partial void OnSelectedAddonsTabIndexChanged(int value) => NotifyTabState();

    private void NotifyTabState()
    {
        OnPropertyChanged(nameof(IsNewGameSection));
        OnPropertyChanged(nameof(IsAddonsSection));
        OnPropertyChanged(nameof(IsGamesSelected));
        OnPropertyChanged(nameof(SelectedTabLabel));
        OnPropertyChanged(nameof(PageStatus));
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnSelectedVersionChanged(VersionSummary? value) =>
        OnPropertyChanged(nameof(CanInstall));

    partial void OnIsInstallingChanged(bool value) =>
        OnPropertyChanged(nameof(CanInstall));

    [RelayCommand]
    private async Task InstallSelectedAsync(CancellationToken cancellationToken)
    {
        if (!CanInstall || SelectedVersion is not { } version)
        {
            return;
        }

        IsInstalling = true;
        InstallProgress = 0;
        Status = $"Preparing Minecraft {version.Id}...";

        try
        {
            var progress = new Progress<InstallProgress>(update =>
            {
                Status = update.Stage;
                InstallProgress = update.Percent;
            });

            var instance = await _installer.InstallAsync(version.Id, progress, cancellationToken);
            await _instances.LoadAsync(cancellationToken);
            _instances.SelectedInstance = _instances.Instances.FirstOrDefault(
                candidate => string.Equals(candidate.Id, instance.Id, StringComparison.Ordinal));

            InstallProgress = 100;
            Status = $"Installed {instance.Name}. Select it on Home to launch.";
        }
        catch (OperationCanceledException)
        {
            Status = "Installation canceled.";
        }
        catch (Exception exception)
        {
            Status = $"Installation failed: {exception.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        Versions.Clear();
        foreach (var version in _allVersions)
        {
            if (query.Length == 0 ||
                version.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                version.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                Versions.Add(version);
            }
        }

        Status = $"{Versions.Count} of {_allVersions.Count} versions";
    }
}
