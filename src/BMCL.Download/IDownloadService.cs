namespace BMCL.Download;

public interface IDownloadService
{
    Task<DownloadResult> DownloadAsync(
        IReadOnlyList<DownloadItem> items,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(string path, string sha1, CancellationToken cancellationToken = default);
}
