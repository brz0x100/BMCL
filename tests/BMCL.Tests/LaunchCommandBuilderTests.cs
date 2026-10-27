using BMCL.Auth;
using BMCL.Core.Minecraft;
using BMCL.Launch;
using Xunit;

namespace BMCL.Tests;

public sealed class LaunchCommandBuilderTests
{
    [Fact]
    public void BuildsModernLaunchCommand()
    {
        var version = CreateModernVersion();
        var command = new LaunchCommandBuilder().Build(version, CreateOptions());

        Assert.Equal("/usr/bin/java", command.JavaPath);
        Assert.Equal("/tmp/bmcl-game", command.WorkingDirectory);
        Assert.Contains("net.minecraft.client.main.Main", command.Arguments);
        Assert.Contains("-Xmx2048M", command.Arguments);

        var arguments = command.Arguments.ToList();
        var classpathIndex = arguments.IndexOf("-cp");
        Assert.True(classpathIndex >= 0);
        Assert.Contains("demo-1.0.jar", arguments[classpathIndex + 1]);
        Assert.Contains("legacy-2.0.jar", arguments[classpathIndex + 1]);
        Assert.DoesNotContain(
            command.Arguments,
            argument => argument.Contains("never-matches.jar", StringComparison.Ordinal));

        var usernameIndex = arguments.IndexOf("--username");
        Assert.True(usernameIndex >= 0);
        Assert.Equal("Steve", arguments[usernameIndex + 1]);

        Assert.Contains(
            command.Arguments,
            argument => argument.StartsWith("-Djava.library.path=/tmp/bmcl-natives", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildsLegacyLaunchCommand()
    {
        var version = new VersionDetail
        {
            Id = "1.7.10",
            Type = "release",
            MainClass = "net.minecraft.client.main.Main",
            MinecraftArguments = "--username ${auth_player_name} --gameDir ${game_directory}",
        };

        var command = new LaunchCommandBuilder().Build(version, CreateOptions());

        var arguments = command.Arguments.ToList();
        var usernameIndex = arguments.IndexOf("--username");
        Assert.True(usernameIndex >= 0);
        Assert.Equal("Steve", arguments[usernameIndex + 1]);

        var gameDirectoryIndex = arguments.IndexOf("--gameDir");
        Assert.True(gameDirectoryIndex >= 0);
        Assert.Equal("/tmp/bmcl-game", arguments[gameDirectoryIndex + 1]);
    }

    [Fact]
    public void AddsLoggingConfiguration()
    {
        var version = CreateModernVersion();
        version = version with
        {
            Logging = new LoggingConfiguration
            {
                Client = new LoggingClient
                {
                    Argument = "-Dlog4j.configurationFile=${path}",
                },
            },
        };

        var options = CreateOptions() with
        {
            LoggingConfigurationPath = "/tmp/bmcl-assets/log_config.xml",
        };

        var command = new LaunchCommandBuilder().Build(version, options);

        Assert.Contains(
            "-Dlog4j.configurationFile=/tmp/bmcl-assets/log_config.xml",
            command.Arguments);
    }

    [Fact]
    public void RejectsVersionsWithoutMainClass()
    {
        var version = CreateModernVersion() with { MainClass = "" };

        Assert.Throws<InvalidOperationException>(
            () => new LaunchCommandBuilder().Build(version, CreateOptions()));
    }

    private static LaunchOptions CreateOptions() => new()
    {
        Account = OfflineAccountFactory.Create("Steve"),
        GameDirectory = "/tmp/bmcl-game",
        AssetsDirectory = "/tmp/bmcl-assets",
        LibrariesDirectory = "/tmp/bmcl-libraries",
        NativesDirectory = "/tmp/bmcl-natives",
        ClientJarPath = "/tmp/bmcl-game/client.jar",
        JavaPath = "/usr/bin/java",
        VersionName = "1.20.1",
    };

    private static VersionDetail CreateModernVersion()
    {
        var library = new MinecraftLibrary
        {
            Name = "com.example:demo:1.0",
            Downloads = new LibraryDownloads
            {
                Artifact = new DownloadArtifact
                {
                    Path = "com/example/demo/1.0/demo-1.0.jar",
                },
            },
        };

        var excludedLibrary = new MinecraftLibrary
        {
            Name = "com.example:never:1.0",
            Rules =
            [
                new Rule
                {
                    Action = "allow",
                    Os = new RuleOs { Name = "never-matches" },
                },
            ],
            Downloads = new LibraryDownloads
            {
                Artifact = new DownloadArtifact
                {
                    Path = "com/example/never/1.0/never-matches.jar",
                },
            },
        };

        return new VersionDetail
        {
            Id = "1.20.1",
            Type = "release",
            MainClass = "net.minecraft.client.main.Main",
            AssetIndex = new AssetIndex { Id = "5", Url = "https://example.invalid/5.json" },
            Libraries =
            [
                library,
                new MinecraftLibrary { Name = "com.example:legacy:2.0" },
                excludedLibrary,
            ],
            Arguments = new VersionArguments
            {
                Game = [new VersionArgument(["--username", "${auth_player_name}"], [])],
                Jvm =
                [
                    new VersionArgument(["-Djava.library.path=${natives_directory}"], []),
                    new VersionArgument(["-cp", "${classpath}"], []),
                ],
            },
        };
    }
}
