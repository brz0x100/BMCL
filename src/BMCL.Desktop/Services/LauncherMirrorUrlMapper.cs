using BMCL.Download;

namespace BMCL.Desktop.Services;

public sealed class LauncherMirrorUrlMapper : IMirrorUrlMapper
{
    private static readonly IMirrorUrlMapper BmclApi = new BmclApiMirrorUrlMapper();
    private readonly LauncherSettingsService _settings;

    public LauncherMirrorUrlMapper(LauncherSettingsService settings)
    {
        _settings = settings;
    }

    public Uri Map(Uri officialUrl) =>
        _settings.Settings.UseBmclApi ? BmclApi.Map(officialUrl) : officialUrl;
}
