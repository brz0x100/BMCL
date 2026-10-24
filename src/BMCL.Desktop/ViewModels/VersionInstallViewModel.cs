using BMCL.Core.Configuration;
using BMCL.Core.Minecraft;
using BMCL.Desktop.Services;
using BMCL.Install;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace BMCL.Desktop.ViewModels;

public sealed partial class VersionInstallViewModel : ViewModelBase
{
    private readonly MinecraftInstaller _installer;
    private readonly InstancesViewModel _instances;
    private readonly LauncherSettingsService _settings;
    private readonly IReadOnlyList<LoaderOption> _loaders;

    public VersionInstallViewModel(
        MinecraftInstaller installer,
        InstancesViewModel instances,
        LauncherSettingsService settings,
        VersionSummary version)
    {
        _installer = installer;
        _instances = instances;
        _settings = settings;
        Version = version;
        InstanceName = version.Id;
        _loaders =
        [
            new LoaderOption("Minecraft", version.Id, MaterialIconKind.Minecraft, true),
            new LoaderOption("Forge", "Do not install", MaterialIconKind.Anvil, false),
            new LoaderOption("NeoForge", "Do not install", MaterialIconKind.CubeOutline, false),
            new LoaderOption("OptiFine", "Do not install", MaterialIconKind.AutoFix, false),
            new LoaderOption("Fabric", "Do not install", MaterialIconKind.VectorSquare, false),
            new LoaderOption("Fabric API", "Do not install", MaterialIconKind.Layers, false),
            new LoaderOption("Quilt", "Do not install", MaterialIconKind.Puzzle, false),
            new LoaderOption("QSL/QFAPI", "Do not install", MaterialIconKind.CodeBraces, false),
        ];
    }

    public VersionSummary Version { get; }

    public IReadOnlyList<LoaderOption> Loaders => _loaders;

    public string InstallButtonText => IsInstalling ? "Installing..." : "Install";

    public bool CanInstall => !IsInstalling && !string.IsNullOrWhiteSpace(InstanceName);

    [ObservableProperty]
    private string _instanceName;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private double _installProgress;

    [ObservableProperty]
    private string _status = "Ready to install";

    partial void OnInstanceNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnIsInstallingChanged(bool value)
    {
        OnPropertyChanged(nameof(InstallButtonText));
        OnPropertyChanged(nameof(CanInstall));
    }

    [RelayCommand]
    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        if (!CanInstall)
        {
            return;
        }

        IsInstalling = true;
        InstallProgress = 0;
        Status = $"Preparing Minecraft {Version.Id}...";

        try
        {
            var progress = new Progress<InstallProgress>(update =>
            {
                Status = update.Stage;
                InstallProgress = update.Percent;
            });

            var instance = await _installer.InstallAsync(
                Version.Id,
                InstanceName,
                _settings.Settings.DefaultIsolationStrategy,
                progress,
                cancellationToken);

            await _instances.LoadAsync(cancellationToken);
            _instances.SelectInstance(instance.Id);

            InstallProgress = 100;
            Status = $"Installed {instance.Name}.";
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
}
