using System.Collections.ObjectModel;
using System.Net.Http;
using BMCL.Core.Minecraft;
using BMCL.Desktop.Services;
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
    private readonly LauncherSettingsService _settings;
    private readonly List<VersionSummary> _allVersions = [];

    public DownloadsViewModel(
        MinecraftVersionService versionService,
        MinecraftInstaller installer,
        InstancesViewModel instances,
        LauncherSettingsService settings)
    {
        _versionService = versionService;
        _installer = installer;
        _instances = instances;
        _settings = settings;
    }

    public ObservableCollection<VersionSummary> Versions { get; } = new();

    [ObservableProperty]
    private VersionSummary? _selectedVersion;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _selectedTypeIndex = 1;

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

    public VersionInstallViewModel CreateInstallViewModel(VersionSummary version) =>
        new(_installer, _instances, _settings, version);

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

    partial void OnSelectedTypeIndexChanged(int value) => ApplyFilter();

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
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        Versions.Clear();
        foreach (var version in _allVersions)
        {
            var matchesSearch = query.Length == 0 ||
                                version.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                version.Type.Contains(query, StringComparison.OrdinalIgnoreCase);
            if (matchesSearch && MatchesType(version, SelectedTypeIndex))
            {
                Versions.Add(version);
            }
        }

        Status = $"{Versions.Count} of {_allVersions.Count} versions";
    }

    private static bool MatchesType(VersionSummary version, int typeIndex) => typeIndex switch
    {
        0 => true,
        1 => string.Equals(version.Type, "release", StringComparison.OrdinalIgnoreCase) &&
             !IsAprilFools(version),
        2 => string.Equals(version.Type, "snapshot", StringComparison.OrdinalIgnoreCase) &&
             !IsAprilFools(version),
        3 => IsAprilFools(version),
        4 => string.Equals(version.Type, "old_alpha", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(version.Type, "old_beta", StringComparison.OrdinalIgnoreCase),
        _ => true,
    };

    private static bool IsAprilFools(VersionSummary version) =>
        string.Equals(version.Id, "2.0", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(version.Id, "1.RV-Pre1", StringComparison.OrdinalIgnoreCase) ||
        (string.Equals(version.Type, "snapshot", StringComparison.OrdinalIgnoreCase) &&
         version.ReleaseTime.Month == 4 &&
         version.ReleaseTime.Day == 1);
}
