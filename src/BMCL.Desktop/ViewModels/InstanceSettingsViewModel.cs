using BMCL.Core.Instances;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace BMCL.Desktop.ViewModels;

public sealed partial class InstanceSettingsViewModel : ViewModelBase
{
    private static readonly string[] TabTitles =
    [
        "Settings",
        "Loaders",
        "Mods",
        "Resource Packs",
        "Worlds",
        "Schematics",
        "Manage",
    ];

    private static readonly MaterialIconKind[] TabIcons =
    [
        MaterialIconKind.Cog,
        MaterialIconKind.CubeOutline,
        MaterialIconKind.Puzzle,
        MaterialIconKind.Texture,
        MaterialIconKind.Earth,
        MaterialIconKind.FileTree,
        MaterialIconKind.Tune,
    ];

    private readonly InstanceStore _store;
    private Instance _instance;

    public InstanceSettingsViewModel(
        Instance instance,
        InstanceStore store,
        int defaultMinMemoryMb,
        int defaultMaxMemoryMb)
    {
        _instance = instance;
        _store = store;
        InstanceName = instance.Name;
        VersionId = instance.VersionId;
        MinMemoryMb = instance.MinMemoryMb ?? defaultMinMemoryMb;
        MaxMemoryMb = instance.MemoryMb ?? defaultMaxMemoryMb;
    }

    public string InstanceName { get; }

    public string VersionId { get; }

    public string InstanceDescription => $"{InstanceName} · {VersionId}";

    public string IsolationText => _instance.Isolated ? "Isolated" : "Shared";

    public string IsolationDescription => _instance.Isolated
        ? "Game data is stored in the instance's version folder."
        : "Game data is shared in the .minecraft root folder.";

    public bool IsSettingsTab => SelectedTabIndex == 1;

    public bool IsPlaceholderTab => !IsSettingsTab;

    public string SelectedTabTitle =>
        TabTitles[Math.Clamp(SelectedTabIndex - 1, 0, TabTitles.Length - 1)];

    public MaterialIconKind PlaceholderIcon =>
        TabIcons[Math.Clamp(SelectedTabIndex - 1, 0, TabIcons.Length - 1)];

    [ObservableProperty]
    private int _selectedTabIndex = 1;

    [ObservableProperty]
    private decimal? _minMemoryMb;

    [ObservableProperty]
    private decimal? _maxMemoryMb;

    [ObservableProperty]
    private string _status = "Settings loaded";

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsPlaceholderTab));
        OnPropertyChanged(nameof(SelectedTabTitle));
        OnPropertyChanged(nameof(PlaceholderIcon));
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var minMemory = (int)(MinMemoryMb ?? 512);
        var maxMemory = (int)(MaxMemoryMb ?? 4096);
        if (minMemory < 0 || maxMemory <= 0 || minMemory > maxMemory)
        {
            Status = "Memory values are invalid.";
            return;
        }

        try
        {
            _instance = _instance with
            {
                MinMemoryMb = minMemory,
                MemoryMb = maxMemory,
            };
            await _store.SaveAsync(_instance, cancellationToken);
            Status = "Settings saved.";
        }
        catch (Exception exception)
        {
            Status = $"Failed to save settings: {exception.Message}";
        }
    }
}
