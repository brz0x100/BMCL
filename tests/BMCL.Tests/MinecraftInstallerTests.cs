using System.IO.Compression;
using System.Text.Json;
using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using BMCL.Core.Minecraft;
using BMCL.Download;
using BMCL.Install;
using Xunit;

namespace BMCL.Tests;

public sealed class MinecraftInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "bmcl-installer-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InstallsVersionFilesAndCreatesInstance()
    {
        var paths = new LauncherPaths(_root);
        paths.EnsureCreated();
        var versionDirectory = Path.Combine(paths.VersionsDirectory, "test-version");
        Directory.CreateDirectory(versionDirectory);
        var version = CreateVersion();
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "test-version.json"),
            JsonSerializer.Serialize(version, MinecraftJson.Options));

        var downloadService = new FakeDownloadService();
        var versionService = new MinecraftVersionService(
            new HttpClient(),
            paths,
            new IdentityMirrorUrlMapper());
        var instanceStore = new InstanceStore(paths);
        var installer = new MinecraftInstaller(
            versionService,
            downloadService,
            paths,
            instanceStore);

        var progress = new List<InstallProgress>();
        var instance = await installer.InstallAsync(
            "test-version",
            "My Test Instance",
            IsolationStrategy.Always,
            new Progress<InstallProgress>(progress.Add));

        Assert.Equal("test-version", instance.Id);
        Assert.Equal("test-version", instance.VersionId);
        Assert.Equal("My Test Instance", instance.Name);
        Assert.True(instance.Isolated);
        Assert.True(File.Exists(Path.Combine(paths.VersionsDirectory, instance.Id, "instance.json")));
        Assert.True(File.Exists(Path.Combine(paths.VersionsDirectory, "test-version", "test-version.jar")));
        Assert.True(File.Exists(Path.Combine(paths.LibrariesDirectory, "org/example/demo/1.0/demo-1.0.jar")));
        Assert.True(File.Exists(Path.Combine(paths.LibrariesDirectory, "org/example/legacy/2.0/legacy-2.0.jar")));
        Assert.True(File.Exists(Path.Combine(paths.VersionsDirectory, instance.Id, "natives", "native.txt")));
        Assert.True(File.Exists(Path.Combine(paths.AssetsDirectory, "indexes", "test-index.json")));
        Assert.True(File.Exists(Path.Combine(paths.AssetsDirectory, "objects", "00", new string('0', 40))));
        Assert.True(File.Exists(Path.Combine(paths.AssetsDirectory, "log_configs", "client.xml")));
        Assert.NotEmpty(progress);

        var shared = await installer.InstallAsync(
            "test-version",
            "Shared Instance",
            IsolationStrategy.Never,
            new Progress<InstallProgress>(_ => { }));

        Assert.False(shared.Isolated);
        Assert.True(File.Exists(Path.Combine(paths.MinecraftDirectory, "natives", "native.txt")));
    }

    [Fact]
    public async Task FailedInstallDoesNotExposePartialVersionAndCanBeRetried()
    {
        var paths = new LauncherPaths(_root);
        paths.EnsureCreated();
        var versionDirectory = Path.Combine(paths.VersionsDirectory, "test-version");
        Directory.CreateDirectory(versionDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "test-version.json"),
            JsonSerializer.Serialize(CreateVersion(), MinecraftJson.Options));

        var versionService = new MinecraftVersionService(
            new HttpClient(),
            paths,
            new IdentityMirrorUrlMapper());
        var instanceStore = new InstanceStore(paths);
        var failingInstaller = new MinecraftInstaller(
            versionService,
            new ThrowingDownloadService(),
            paths,
            instanceStore);

        await Assert.ThrowsAsync<IOException>(() => failingInstaller.InstallAsync("test-version"));
        Assert.Empty(await instanceStore.LoadAsync());

        var installer = new MinecraftInstaller(
            versionService,
            new FakeDownloadService(),
            paths,
            instanceStore);

        var instance = await installer.InstallAsync("test-version");

        var loaded = Assert.Single(await instanceStore.LoadAsync());
        Assert.Equal(instance, loaded);
        Assert.False(File.Exists(Path.Combine(
            paths.VersionsDirectory,
            "test-version",
            InstanceStore.InstallationMarkerFileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static VersionDetail CreateVersion()
    {
        var os = RuleEvaluator.GetCurrentOsName();
        var classifier = $"natives-{os}";
        var assetHash = new string('0', 40);

        return new VersionDetail
        {
            Id = "test-version",
            Type = "release",
            MainClass = "net.minecraft.client.main.Main",
            Downloads = new VersionDownloads
            {
                Client = new DownloadArtifact
                {
                    Url = "https://example.test/client.jar",
                    Sha1 = new string('1', 40),
                    Size = 1,
                },
            },
            Libraries =
            [
                new MinecraftLibrary
                {
                    Name = "org.example:demo:1.0",
                    Downloads = new LibraryDownloads
                    {
                        Artifact = new DownloadArtifact
                        {
                            Path = "org/example/demo/1.0/demo-1.0.jar",
                            Url = "https://example.test/demo.jar",
                            Sha1 = new string('2', 40),
                            Size = 1,
                        },
                    },
                },
                new MinecraftLibrary
                {
                    Name = "org.example:legacy:2.0",
                },
                new MinecraftLibrary
                {
                    Name = "org.example:native:1.0",
                    Natives = new Dictionary<string, string>
                    {
                        [os] = classifier,
                    },
                    Downloads = new LibraryDownloads
                    {
                        Classifiers = new Dictionary<string, DownloadArtifact>
                        {
                            [classifier] = new DownloadArtifact
                            {
                                Path = $"org/example/native/1.0/native-1.0-{classifier}.jar",
                                Url = "https://example.test/native.jar",
                                Sha1 = new string('3', 40),
                                Size = 1,
                            },
                        },
                    },
                },
            ],
            AssetIndex = new AssetIndex
            {
                Id = "test-index",
                Url = "https://example.test/index.json",
                Sha1 = new string('4', 40),
                Size = 1,
            },
            Logging = new LoggingConfiguration
            {
                Client = new LoggingClient
                {
                    File = new LoggingFile
                    {
                        Id = "client.xml",
                        Url = "https://example.test/log.xml",
                        Sha1 = new string('5', 40),
                        Size = 1,
                    },
                },
            },
            Arguments = new VersionArguments
            {
                Game = [new VersionArgument(["--username", "${auth_player_name}"], [])],
            },
        };
    }

    private sealed class ThrowingDownloadService : IDownloadService
    {
        public Task<DownloadResult> DownloadAsync(
            IReadOnlyList<DownloadItem> items,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new IOException("Simulated download failure.");

        public Task<bool> VerifyAsync(
            string path,
            string sha1,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeDownloadService : IDownloadService
    {
        public Task<DownloadResult> DownloadAsync(
            IReadOnlyList<DownloadItem> items,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (File.Exists(item.Destination))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(item.Destination)!;
                Directory.CreateDirectory(directory);

                if (item.Destination.EndsWith("indexes/test-index.json", StringComparison.Ordinal))
                {
                    File.WriteAllText(
                        item.Destination,
                        """{"objects":{"asset":{"hash":"0000000000000000000000000000000000000000","size":1}}}""");
                }
                else if (item.Destination.EndsWith("log_configs/client.xml", StringComparison.Ordinal))
                {
                    File.WriteAllText(item.Destination, "<configuration />");
                }
                else if (item.Destination.Contains(
                             $"{Path.DirectorySeparatorChar}assets{Path.DirectorySeparatorChar}objects{Path.DirectorySeparatorChar}",
                             StringComparison.Ordinal))
                {
                    File.WriteAllBytes(item.Destination, [1]);
                }
                else if (item.Destination.EndsWith(".jar", StringComparison.Ordinal) &&
                         item.Destination.Contains("native", StringComparison.OrdinalIgnoreCase))
                {
                    using var archive = ZipFile.Open(item.Destination, ZipArchiveMode.Create);
                    var entry = archive.CreateEntry("native.txt");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write("native");
                }
                else
                {
                    File.WriteAllBytes(item.Destination, [1]);
                }
            }

            return Task.FromResult(new DownloadResult(items.Count, 0));
        }

        public Task<bool> VerifyAsync(
            string path,
            string sha1,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(File.Exists(path));
    }
}
