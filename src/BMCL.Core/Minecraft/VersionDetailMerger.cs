namespace BMCL.Core.Minecraft;

public static class VersionDetailMerger
{
    public static VersionDetail Merge(VersionDetail parent, VersionDetail child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        return child with
        {
            Type = string.IsNullOrWhiteSpace(child.Type) ? parent.Type : child.Type,
            MainClass = string.IsNullOrWhiteSpace(child.MainClass)
                ? parent.MainClass
                : child.MainClass,
            Assets = child.Assets ?? parent.Assets,
            AssetIndex = child.AssetIndex ?? parent.AssetIndex,
            Downloads = child.Downloads?.Client is not null ? child.Downloads : parent.Downloads,
            Libraries = MergeLibraries(parent.Libraries, child.Libraries),
            Arguments = MergeArguments(parent.Arguments, child.Arguments),
            MinecraftArguments = string.IsNullOrWhiteSpace(child.MinecraftArguments)
                ? parent.MinecraftArguments
                : child.MinecraftArguments,
            JavaVersion = child.JavaVersion is { MajorVersion: > 0 }
                ? child.JavaVersion
                : parent.JavaVersion,
            Logging = child.Logging?.Client is not null ? child.Logging : parent.Logging,
            ComplianceLevel = child.ComplianceLevel ?? parent.ComplianceLevel,
        };
    }

    private static IReadOnlyList<MinecraftLibrary> MergeLibraries(
        IReadOnlyList<MinecraftLibrary> parentLibraries,
        IReadOnlyList<MinecraftLibrary> childLibraries)
    {
        var overrides = new Dictionary<string, MinecraftLibrary>(StringComparer.Ordinal);
        foreach (var library in childLibraries)
        {
            overrides[library.Name] = library;
        }

        var merged = new List<MinecraftLibrary>(parentLibraries.Count + overrides.Count);
        foreach (var library in parentLibraries)
        {
            if (overrides.Remove(library.Name, out var replacement))
            {
                merged.Add(replacement);
            }
            else
            {
                merged.Add(library);
            }
        }

        merged.AddRange(overrides.Values);
        return merged;
    }

    private static VersionArguments? MergeArguments(
        VersionArguments? parentArguments,
        VersionArguments? childArguments)
    {
        if (parentArguments is null)
        {
            return childArguments;
        }

        if (childArguments is null)
        {
            return parentArguments;
        }

        return new VersionArguments
        {
            Game = [.. parentArguments.Game, .. childArguments.Game],
            Jvm = [.. parentArguments.Jvm, .. childArguments.Jvm],
        };
    }
}
