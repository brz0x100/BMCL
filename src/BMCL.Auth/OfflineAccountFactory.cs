using System.Security.Cryptography;
using System.Text;

namespace BMCL.Auth;

public static class OfflineAccountFactory
{
    public static MinecraftAccount Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A player name is required.", nameof(name));
        }

        return new MinecraftAccount
        {
            Name = name,
            Uuid = ComputeOfflineUuid(name),
            Type = AccountType.Offline,
        };
    }

    public static string ComputeOfflineUuid(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{name}"));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
