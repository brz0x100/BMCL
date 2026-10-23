namespace BMCL.Download;

public interface IMirrorUrlMapper
{
    Uri Map(Uri officialUrl);
}

public sealed class IdentityMirrorUrlMapper : IMirrorUrlMapper
{
    public Uri Map(Uri officialUrl) => officialUrl;
}
