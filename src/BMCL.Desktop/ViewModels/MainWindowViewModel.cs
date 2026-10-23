using System.Collections.ObjectModel;
using System.Net.Http;
using BMCL.Core.Minecraft;
using BMCL.Download;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BMCL.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly MinecraftVersionService _versionService;

    public MainWindowViewModel(MinecraftVersionService versionService)
    {
        _versionService = versionService;
    }

    public ObservableCollection<VersionSummary> Versions { get; } = new();

    [ObservableProperty]
    private string _status = "Version list not loaded";

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Status = "Loading version list...";

        try
        {
            var manifest = await _versionService.GetManifestAsync(
                forceRefresh: true,
                cancellationToken);

            Versions.Clear();
            foreach (var version in manifest.Versions.OrderByDescending(version => version.ReleaseTime))
            {
                Versions.Add(version);
            }

            Status = $"Loaded {Versions.Count} versions";
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
            Status = $"Failed to load: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
