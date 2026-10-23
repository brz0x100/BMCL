using BMCL.Auth;
using Xunit;

namespace BMCL.Tests;

public sealed class OfflineAccountFactoryTests
{
    [Fact]
    public void ProducesKnownOfflineUuid()
    {
        Assert.Equal("5627dd98e6be3c21b8a8e92344183641", OfflineAccountFactory.ComputeOfflineUuid("Steve"));
        Assert.Equal("b50ad385829d3141a2167e7d7539ba7f", OfflineAccountFactory.ComputeOfflineUuid("Notch"));
    }

    [Fact]
    public void ProducesVersionThreeUuid()
    {
        var uuid = OfflineAccountFactory.ComputeOfflineUuid("Alex");

        Assert.Equal(32, uuid.Length);
        Assert.Equal('3', uuid[12]);
        Assert.True("89ab".Contains(uuid[16]));
    }

    [Fact]
    public void CreatesOfflineAccount()
    {
        var account = OfflineAccountFactory.Create("Steve");

        Assert.Equal("Steve", account.Name);
        Assert.Equal(AccountType.Offline, account.Type);
        Assert.Equal("5627dd98e6be3c21b8a8e92344183641", account.Uuid);
        Assert.Null(account.AccessToken);
    }

    [Fact]
    public void RejectsEmptyNames()
    {
        Assert.Throws<ArgumentException>(() => OfflineAccountFactory.Create(" "));
    }
}
