using BMCL.Core.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BMCL.Desktop.Services;

public sealed class LauncherSettingsService : ObservableObject
{
    private readonly LauncherSettingsStore _store;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public LauncherSettingsService(LauncherSettingsStore store)
    {
        _store = store;
    }

    public LauncherSettings Settings { get; private set; } = new();

    public bool IsLoaded { get; private set; }

    public event EventHandler? Changed;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Exception? loadError = null;

        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            try
            {
                Settings = await _store.LoadAsync(cancellationToken);
            }
            catch (InvalidDataException exception)
            {
                Settings = new LauncherSettings();
                loadError = exception;
            }

            if (!File.Exists(_store.FilePath))
            {
                await _store.SaveAsync(Settings, cancellationToken);
            }
        }
        finally
        {
            _saveLock.Release();
        }

        IsLoaded = true;
        NotifyChanged();

        if (loadError is not null)
        {
            throw new InvalidDataException(
                "The settings file could not be read; defaults are being used.",
                loadError);
        }
    }

    public async Task SaveAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            await _store.SaveAsync(settings, cancellationToken);
            Settings = settings;
            NotifyChanged();
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void NotifyChanged()
    {
        OnPropertyChanged(nameof(Settings));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
