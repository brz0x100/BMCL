namespace BMCL.Download;

public sealed record DownloadItem
{
    public DownloadItem(Uri url, string destination, string? sha1 = null, long? size = null)
    {
        Url = url;
        Destination = destination;
        Sha1 = sha1;
        Size = size;
    }

    public Uri Url { get; }

    public string Destination { get; }

    public string? Sha1 { get; }

    public long? Size { get; }
}
