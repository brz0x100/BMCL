using System.IO.Compression;
using BMCL.Core.Minecraft;
using BMCL.Launch;
using Xunit;

namespace BMCL.Tests;

public sealed class NativesExtractorTests
{
    [Fact]
    public void ExtractsNativeFilesAndSkipsUnsafeEntries()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bmcl-natives-{Guid.NewGuid():N}");
        var libraries = Path.Combine(root, "libraries");
        var natives = Path.Combine(root, "natives");
        var relativeJarPath = "org/lwjgl/lwjgl/3.3.1/lwjgl-3.3.1-natives-windows.jar";
        var jarPath = Path.Combine(libraries, relativeJarPath.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(jarPath)!);
            using (var archive = ZipFile.Open(jarPath, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "lwjgl.dll", "native");
                WriteEntry(archive, "META-INF/MANIFEST.MF", "ignored");
                WriteEntry(archive, "../escape.txt", "unsafe");
            }

            var version = new VersionDetail
            {
                Libraries =
                [
                    new MinecraftLibrary
                    {
                        Name = "org.lwjgl:lwjgl:3.3.1",
                        Natives = new Dictionary<string, string>
                        {
                            ["windows"] = "natives-windows",
                        },
                        Downloads = new LibraryDownloads
                        {
                            Classifiers = new Dictionary<string, DownloadArtifact>
                            {
                                ["natives-windows"] = new DownloadArtifact
                                {
                                    Path = relativeJarPath,
                                },
                            },
                        },
                    },
                ],
            };

            var extracted = NativesExtractor.Extract(
                version,
                new RuleContext("windows", "x86_64"),
                libraries,
                natives);

            Assert.Equal(1, extracted);
            Assert.Equal("native", File.ReadAllText(Path.Combine(natives, "lwjgl.dll")));
            Assert.False(File.Exists(Path.Combine(root, "escape.txt")));
            Assert.False(File.Exists(Path.Combine(natives, "escape.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
