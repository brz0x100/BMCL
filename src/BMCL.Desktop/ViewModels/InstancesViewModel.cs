using System.Collections.ObjectModel;
using BMCL.Core.Instances;
using BMCL.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace BMCL.Desktop.ViewModels;

public sealed partial class InstancesViewModel : ViewModelBase
{
    private readonly InstanceStore _store;
    private readonly LauncherSettingsService _settings;
    private readonly List<Instance> _allInstances = [];

    public InstancesViewModel(InstanceStore store, LauncherSettingsService settings)
    {
        _store = store;
        _settings = settings;
        Instances.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasInstances));
    }

    public ObservableCollection<Instance> Instances { get; } = new();

    public bool HasInstances => Instances.Count > 0;

    public string EmptyStateTitle => _allInstances.Count == 0
        ? "No instances yet"
        : "No matching instances";

    public string EmptyStateMessage => _allInstances.Count == 0
        ? "Install a version from Downloads to create one."
        : "Try a different search term.";

    public string DisplayName => SelectedInstance?.Name ?? "Instance Name";

    public string DisplayVersion => SelectedInstance is null
        ? "Minecraft Version + Loader"
        : SelectedInstance.VersionId;

    public MaterialIconKind SortIcon => SortDescending
        ? MaterialIconKind.SortDescending
        : MaterialIconKind.SortAscending;

    [ObservableProperty]
    private Instance? _selectedInstance;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _selectedSortIndex;

    [ObservableProperty]
    private bool _sortDescending;

    [ObservableProperty]
    private string _status = "No instances yet";

    [ObservableProperty]
    private bool _isBusy;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Status = "Loading instances...";

        try
        {
            var selectedId = SelectedInstance?.Id;
            var instances = await _store.LoadAsync(cancellationToken);

            _allInstances.Clear();
            _allInstances.AddRange(instances);
            ApplyView(selectedId);

            Status = _allInstances.Count == 0
                ? "No instances yet"
                : $"Loaded {_allInstances.Count} instances";
        }
        catch (OperationCanceledException)
        {
            Status = "Canceled";
        }
        catch (Exception exception)
        {
            Status = $"Failed to load instances: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectInstance(string id)
    {
        SearchText = "";
        ApplyView(id);
    }

    public InstanceSettingsViewModel CreateSettingsViewModel(Instance instance) =>
        new(
            instance,
            _store,
            _settings.Settings.MinMemoryMb,
            _settings.Settings.MaxMemoryMb);

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    [RelayCommand]
    private void ToggleSortDirection() => SortDescending = !SortDescending;

    partial void OnSelectedInstanceChanged(Instance? value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayVersion));
    }

    partial void OnSearchTextChanged(string value) => ApplyView();

    partial void OnSelectedSortIndexChanged(int value) => ApplyView();

    partial void OnSortDescendingChanged(bool value)
    {
        OnPropertyChanged(nameof(SortIcon));
        ApplyView();
    }

    private void ApplyView(string? preferredId = null)
    {
        var query = SearchText.Trim();
        var matching = _allInstances.Where(instance =>
            query.Length == 0 ||
            instance.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            instance.VersionId.Contains(query, StringComparison.OrdinalIgnoreCase));

        var sorted = SelectedSortIndex switch
        {
            1 => matching.OrderBy(instance => instance.LastPlayedAt ?? instance.CreatedAt),
            2 => matching.OrderBy(instance => instance.CreatedAt),
            3 => matching.OrderBy(instance => instance.VersionId, StringComparer.CurrentCultureIgnoreCase),
            _ => matching.OrderBy(instance => instance.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        var ordered = SortDescending ? sorted.Reverse() : sorted;
        var preferredSelection = preferredId ?? SelectedInstance?.Id;

        Instances.Clear();
        foreach (var instance in ordered)
        {
            Instances.Add(instance);
        }

        OnPropertyChanged(nameof(HasInstances));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));

        SelectedInstance = Instances.FirstOrDefault(
                               instance => string.Equals(
                                   instance.Id,
                                   preferredSelection,
                                   StringComparison.Ordinal))
                           ?? Instances.FirstOrDefault();
    }
}
