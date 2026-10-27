using BMCL.Download;
using Xunit;

namespace BMCL.Tests;

public sealed class BmclApiMirrorUrlMapperTests
{
    private readonly BmclApiMirrorUrlMapper _mapper = new();

    [Theory]
    [InlineData(
        "https://piston-meta.mojang.com/v1/packages/hash/1.21.4.json",
        "https://bmclapi2.bangbang93.com/v1/packages/hash/1.21.4.json")]
    [InlineData(
        "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json",
        "https://bmclapi2.bangbang93.com/mc/game/version_manifest_v2.json")]
    [InlineData(
        "https://piston-data.mojang.com/v1/objects/hash/client.jar",
        "https://bmclapi2.bangbang93.com/v1/objects/hash/client.jar")]
    public void MapsMetadataHosts(string source, string expected)
    {
        Assert.Equal(expected, _mapper.Map(new Uri(source)).AbsoluteUri);
    }

    [Fact]
    public void MapsLibraryAndAssetBases()
    {
        Assert.Equal(
            "https://bmclapi2.bangbang93.com/maven/org/lwjgl/lwjgl/3.3.1/lwjgl-3.3.1.jar",
            _mapper.Map(new Uri(
                "https://libraries.minecraft.net/org/lwjgl/lwjgl/3.3.1/lwjgl-3.3.1.jar")).AbsoluteUri);

        Assert.Equal(
            "https://bmclapi2.bangbang93.com/assets/ab/cdef",
            _mapper.Map(new Uri(
                "https://resources.download.minecraft.net/ab/cdef")).AbsoluteUri);
    }

    [Fact]
    public void LeavesUnknownHostsUntouched()
    {
        var source = new Uri("https://example.test/file.jar");

        Assert.Same(source, _mapper.Map(source));
    }
}
