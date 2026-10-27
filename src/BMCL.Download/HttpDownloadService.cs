using System.Security.Cryptography;

namespace BMCL.Download;

public sealed class HttpDownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IMirrorUrlMapper _mirror;
    private readonly Func<int> _maxConcurrency;

    public HttpDownloadService(
        HttpClient httpClient,
        IMirrorUrlMapper mirror,
        int maxConcurrency = 8)
        : this(httpClient, mirror, () => maxConcurrency)
    {
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        }
    }

    public HttpDownloadService(
        HttpClient httpClient,
        IMirrorUrlMapper mirror,
        Func<int> maxConcurrency)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        _maxConcurrency = maxConcurrency ?? throw new ArgumentNullException(nameof(maxConcurrency));
    }

    public async Task<DownloadResult> DownloadAsync(
        IReadOnlyList<DownloadItem> items,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return new DownloadResult(0, 0);
        }

        var maxConcurrency = _maxConcurrency();
        if (maxConcurrency < 1)
        {
            throw new InvalidOperationException("The maximum download concurrency must be at least one.");
        }

        var downloaded = 0;
        var skipped = 0;
        var completed = 0;

        await Parallel.ForEachAsync(
            items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken,
            },
            async (item, token) =>
            {
                var wasDownloaded = await EnsureFileAsync(item, token);
                if (wasDownloaded)
                {
                    Interlocked.Increment(ref downloaded);
                }
                else
                {
                    Interlocked.Increment(ref skipped);
                }

                var done = Interlocked.Increment(ref completed);
                progress?.Report(new DownloadProgress(done, items.Count, Path.GetFileName(item.Destination)));
            });

        return new DownloadResult(downloaded, skipped);
    }

    public async Task<bool> VerifyAsync(
        string path,
        string sha1,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        await using var stream = File.OpenRead(path);
        var hash = await SHA1.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).Equals(sha1, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> EnsureFileAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        var destination = Path.GetFullPath(item.Destination);

        if (File.Exists(destination) && await IsCurrentAsync(destination, item, cancellationToken))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException($"The destination has no directory: {destination}");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{destination}.{Guid.NewGuid():N}.part";
        try
        {
            using var response = await _httpClient.GetAsync(
                _mirror.Map(item.Url),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(target, cancellationToken);
            }

            if (item.Sha1 is { Length: > 0 } expected &&
                !await VerifyAsync(temporaryPath, expected, cancellationToken))
            {
                throw new InvalidDataException($"SHA-1 mismatch for {Path.GetFileName(destination)}.");
            }

            if (item.Size is { } size && new FileInfo(temporaryPath).Length != size)
            {
                throw new InvalidDataException($"Size mismatch for {Path.GetFileName(destination)}.");
            }

            File.Move(temporaryPath, destination, overwrite: true);
            return true;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private async Task<bool> IsCurrentAsync(
        string path,
        DownloadItem item,
        CancellationToken cancellationToken)
    {
        if (item.Size is { } size && new FileInfo(path).Length != size)
        {
            return false;
        }

        return item.Sha1 is not { Length: > 0 } ||
               await VerifyAsync(path, item.Sha1, cancellationToken);
    }
}
