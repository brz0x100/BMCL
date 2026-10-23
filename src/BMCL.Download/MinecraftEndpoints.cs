namespace BMCL.Download;

public static class MinecraftEndpoints
{
    public static Uri VersionManifest { get; } =
        new("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");

    public static Uri AssetBase { get; } = new("https://resources.download.minecraft.net/");

    public static Uri LibraryBase { get; } = new("https://libraries.minecraft.net/");
}
