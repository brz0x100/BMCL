namespace BMCL.Download;

public sealed class BmclApiMirrorUrlMapper : IMirrorUrlMapper
{
    private static readonly Uri BaseUri = new("https://bmclapi2.bangbang93.com/");

    public Uri Map(Uri officialUrl)
    {
        ArgumentNullException.ThrowIfNull(officialUrl);

        if (IsHost(officialUrl, "libraries.minecraft.net"))
        {
            return CreateUri("maven/", officialUrl);
        }

        if (IsHost(officialUrl, "resources.download.minecraft.net"))
        {
            return CreateUri("assets/", officialUrl);
        }

        return IsHost(officialUrl, "launchermeta.mojang.com") ||
               IsHost(officialUrl, "piston-meta.mojang.com") ||
               IsHost(officialUrl, "piston-data.mojang.com")
            ? CreateUri("", officialUrl)
            : officialUrl;
    }

    private static Uri CreateUri(string prefix, Uri original) =>
        new(BaseUri, $"{prefix}{original.AbsolutePath.TrimStart('/')}{original.Query}");

    private static bool IsHost(Uri uri, string host) =>
        string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase);
}
