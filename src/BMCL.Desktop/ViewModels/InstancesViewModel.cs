using System.Collections.ObjectModel;
using BMCL.Core.Instances;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class InstancesViewModel : ViewModelBase
{
    private readonly InstanceStore _store;

    public InstancesViewModel(InstanceStore store)
    {
        _store = store;
        Instances.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasInstances));
    }

    public ObservableCollection<Instance> Instances { get; } = new();

    public bool HasInstances => Instances.Count > 0;

    public string DisplayName => SelectedInstance?.Name ?? "Instance Name";

    public string DisplayVersion => SelectedInstance is null
        ? "Minecraft Version + Loader"
        : SelectedInstance.VersionId;

    [ObservableProperty]
    private Instance? _selectedInstance;

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

            Instances.Clear();
            foreach (var instance in instances)
            {
                Instances.Add(instance);
            }

            SelectedInstance = Instances.FirstOrDefault(
                                   instance => string.Equals(
                                       instance.Id,
                                       selectedId,
                                       StringComparison.Ordinal))
                               ?? Instances.FirstOrDefault();

            Status = Instances.Count == 0
                ? "No instances yet"
                : $"Loaded {Instances.Count} instances";
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

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    partial void OnSelectedInstanceChanged(Instance? value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayVersion));
    }
}
