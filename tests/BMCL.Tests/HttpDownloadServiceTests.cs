using System.Net;
using System.Security.Cryptography;
using BMCL.Download;
using Xunit;

namespace BMCL.Tests;

public sealed class HttpDownloadServiceTests
{
    [Fact]
    public async Task VerifiesMatchingSha1IgnoringCase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bmcl-hash-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(path, "hello");
            var expected = Convert.ToHexString(SHA1.HashData("hello"u8));

            using var httpClient = new HttpClient();
            var service = new HttpDownloadService(httpClient, new IdentityMirrorUrlMapper());

            Assert.True(await service.VerifyAsync(path, expected.ToLowerInvariant()));
            Assert.False(await service.VerifyAsync(path, new string('0', 40)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReportsMissingFileAsInvalid()
    {
        using var httpClient = new HttpClient();
        var service = new HttpDownloadService(httpClient, new IdentityMirrorUrlMapper());

        Assert.False(await service.VerifyAsync(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
            new string('0', 40)));
    }

    [Fact]
    public async Task RedownloadsFileWhenSizeDoesNotMatch()
    {
        var root = CreateTempDirectory();
        try
        {
            var destination = Path.Combine(root, "client.jar");
            await File.WriteAllTextAsync(destination, "old");
            using var handler = new StubHandler(_ => CreateResponse("fresh"));
            using var httpClient = new HttpClient(handler);
            var service = new HttpDownloadService(httpClient, new IdentityMirrorUrlMapper());

            var result = await service.DownloadAsync(
            [
                new DownloadItem(new Uri("https://example.test/client.jar"), destination, size: 5),
            ]);

            Assert.Equal(new DownloadResult(1, 0), result);
            Assert.Equal("fresh", await File.ReadAllTextAsync(destination));
            Assert.Equal(1, handler.RequestCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SkipsExistingFileWithMatchingSizeWhenNoHashIsAvailable()
    {
        var root = CreateTempDirectory();
        try
        {
            var destination = Path.Combine(root, "client.jar");
            await File.WriteAllTextAsync(destination, "fresh");
            using var handler = new StubHandler(_ => throw new InvalidOperationException("No request expected."));
            using var httpClient = new HttpClient(handler);
            var service = new HttpDownloadService(httpClient, new IdentityMirrorUrlMapper());

            var result = await service.DownloadAsync(
            [
                new DownloadItem(new Uri("https://example.test/client.jar"), destination, size: 5),
            ]);

            Assert.Equal(new DownloadResult(0, 1), result);
            Assert.Equal(0, handler.RequestCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RejectsDownloadedFileWithUnexpectedSize()
    {
        var root = CreateTempDirectory();
        try
        {
            var destination = Path.Combine(root, "client.jar");
            using var handler = new StubHandler(_ => CreateResponse("short"));
            using var httpClient = new HttpClient(handler);
            var service = new HttpDownloadService(httpClient, new IdentityMirrorUrlMapper());

            await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(
            [
                new DownloadItem(new Uri("https://example.test/client.jar"), destination, size: 50),
            ]));

            Assert.False(File.Exists(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bmcl-download-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static HttpResponseMessage CreateResponse(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content),
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
