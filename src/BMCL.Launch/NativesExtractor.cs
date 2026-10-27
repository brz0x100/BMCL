using System.IO.Compression;
using BMCL.Core.IO;
using BMCL.Core.Minecraft;

namespace BMCL.Launch;

public static class NativesExtractor
{
    public static int Extract(
        VersionDetail version,
        RuleContext context,
        string librariesDirectory,
        string nativesDirectory)
    {
        ArgumentNullException.ThrowIfNull(version);

        var librariesRoot = Path.GetFullPath(librariesDirectory);
        var nativesRoot = Path.GetFullPath(nativesDirectory);
        Directory.CreateDirectory(nativesRoot);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var extracted = 0;

        foreach (var library in version.Libraries)
        {
            if (!RuleEvaluator.IsAllowed(library.Rules, context) ||
                LibraryFileResolver.ResolveNative(library, context.OsName) is not { } file)
            {
                continue;
            }

            string jarPath;
            try
            {
                jarPath = PathSafety.CombineUnderRoot(librariesRoot, file.RelativePath);
            }
            catch (InvalidDataException)
            {
                continue;
            }

            if (!jarPath.StartsWith(librariesRoot + Path.DirectorySeparatorChar, comparison) ||
                !File.Exists(jarPath))
            {
                continue;
            }

            using var archive = ZipFile.OpenRead(jarPath);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith('/') ||
                    entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase) ||
                    IsExcluded(entry.FullName, library.Extract))
                {
                    continue;
                }

                var destination = Path.GetFullPath(Path.Combine(nativesRoot, entry.FullName));
                var relativeDestination = Path.GetRelativePath(nativesRoot, destination);
                if (relativeDestination.StartsWith("..", StringComparison.Ordinal) ||
                    Path.IsPathRooted(relativeDestination))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: true);
                extracted++;
            }
        }

        return extracted;
    }

    private static bool IsExcluded(string entryName, LibraryExtract? extract) =>
        extract?.Exclude?.Any(prefix =>
            entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ?? false;
}
