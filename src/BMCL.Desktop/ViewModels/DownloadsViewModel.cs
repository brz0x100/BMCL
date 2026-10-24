using System.Collections.ObjectModel;
using System.Net.Http;
using BMCL.Core.Minecraft;
using BMCL.Download;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class DownloadsViewModel : ViewModelBase
{
    private readonly MinecraftVersionService _versionService;
    private readonly List<VersionSummary> _allVersions = [];

    public DownloadsViewModel(MinecraftVersionService versionService)
    {
        _versionService = versionService;
    }

    public ObservableCollection<VersionSummary> Versions { get; } = new();

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
