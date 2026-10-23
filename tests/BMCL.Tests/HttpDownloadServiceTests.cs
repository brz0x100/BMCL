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
}
