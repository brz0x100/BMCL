using System.Diagnostics;
using BMCL.Auth;
using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using BMCL.Core.Minecraft;
using BMCL.Download;
using BMCL.Launch;
using BMCL.Launch.Java;

namespace BMCL.Desktop.Services;

public sealed record GameLaunchResult(string JavaPath, int ProcessId);

public sealed class GameLaunchService
{
    private readonly LauncherSettingsService _settings;
    private readonly AccountManager _accounts;
    private readonly MinecraftVersionService _versions;
    private readonly IJavaLocator _javaLocator;
    private readonly LaunchCommandBuilder _commandBuilder;
    private readonly LauncherPaths _paths;
    private readonly InstanceStore _instances;

    public GameLaunchService(
        LauncherSettingsService settings,
        AccountManager accounts,
        MinecraftVersionService versions,
        IJavaLocator javaLocator,
        LaunchCommandBuilder commandBuilder,
        LauncherPaths paths,
        InstanceStore instances)
    {
        _settings = settings;
        _accounts = accounts;
        _versions = versions;
        _javaLocator = javaLocator;
        _commandBuilder = commandBuilder;
        _paths = paths;
        _instances = instances;
    }

    public async Task<GameLaunchResult> LaunchAsync(
        Instance instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var account = _accounts.SelectedAccount
            ?? throw new InvalidOperationException("Select an account before launching.");

        var version = await _versions.GetVersionAsync(
            instance.VersionId,
            cancellationToken: cancellationToken);

        var clientJarPath = Path.Combine(
            _paths.Versions,
            instance.VersionId,
            $"{instance.VersionId}.jar");
        if (!File.Exists(clientJarPath))
        {
            throw new FileNotFoundException(
                $"Minecraft {instance.VersionId} is not installed. Install it from Downloads first.",
                clientJarPath);
        }

        var javaPath = await ResolveJavaAsync(instance, version, cancellationToken);
        var gameDirectory = Path.Combine(_paths.Instances, instance.Id);
        var nativesDirectory = Path.Combine(gameDirectory, "natives");
        Directory.CreateDirectory(gameDirectory);
        NativesExtractor.Extract(
            version,
            RuleContext.Current,
            _paths.Libraries,
            nativesDirectory);

        var loggingPath = ResolveLoggingPath(version);
        var memoryMb = instance.MemoryMb ?? _settings.Settings.MaxMemoryMb;
        var options = new LaunchOptions
        {
            Account = account,
            GameDirectory = gameDirectory,
            AssetsDirectory = _paths.Assets,
            LibrariesDirectory = _paths.Libraries,
            NativesDirectory = nativesDirectory,
            ClientJarPath = clientJarPath,
            JavaPath = javaPath,
            VersionName = instance.Name,
            AssetIndexName = version.AssetIndex?.Id,
            LoggingConfigurationPath = loggingPath,
            MinMemoryMb = _settings.Settings.MinMemoryMb,
            MaxMemoryMb = memoryMb,
        };

        var command = _commandBuilder.Build(version, options);
        var startInfo = new ProcessStartInfo(command.JavaPath)
        {
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The game process could not be started.");

        await _instances.SaveAsync(
            instance with { LastPlayedAt = DateTimeOffset.UtcNow },
            cancellationToken);

        return new GameLaunchResult(javaPath, process.Id);
    }

    private async Task<string> ResolveJavaAsync(
        Instance instance,
        VersionDetail version,
        CancellationToken cancellationToken)
    {
        var configuredPath = instance.JavaPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = _settings.Settings.JavaPath;
        }

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullPath = Path.GetFullPath(configuredPath.Trim());
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "The configured Java executable does not exist. Update it in Settings > Java Management.",
                    fullPath);
            }

            return fullPath;
        }

        var requiredMajor = version.JavaVersion?.MajorVersion ?? 8;
        var runtimes = await _javaLocator.FindAsync(cancellationToken);
        var runtime = runtimes.FirstOrDefault(candidate => candidate.MajorVersion >= requiredMajor);
        if (runtime is null)
        {
            throw new InvalidOperationException(
                $"A Java {requiredMajor}+ runtime was not found. Set one in Settings > Java Management.");
        }

        return runtime.Path;
    }

    private string? ResolveLoggingPath(VersionDetail version)
    {
        var id = version.Logging?.Client?.File?.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var path = Path.Combine(_paths.Assets, "log_configs", id);
        return File.Exists(path) ? path : null;
    }
}
