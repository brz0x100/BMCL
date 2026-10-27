using BMCL.Auth;
using BMCL.Core.IO;
using BMCL.Core.Minecraft;

namespace BMCL.Launch;

public sealed class LaunchCommandBuilder
{
    public LaunchCommand Build(VersionDetail version, LaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(version.MainClass))
        {
            throw new InvalidOperationException($"Version '{version.Id}' has no main class.");
        }

        if (string.IsNullOrWhiteSpace(options.JavaPath))
        {
            throw new ArgumentException("A Java executable path is required.", nameof(options));
        }

        var features = CreateFeatures(options);
        var context = new RuleContext(
            RuleContext.Current.OsName,
            RuleContext.Current.OsArch,
            RuleContext.Current.OsVersion,
            features);

        var classpath = BuildClasspath(version, options, context);
        var variables = CreateVariables(version, options, classpath);
        var jvmArguments = ExpandArguments(version.Arguments?.Jvm ?? [], context, variables);
        AddJvmDefaults(jvmArguments, options, classpath);
        AddLogging(jvmArguments, version, options);

        jvmArguments.Add(version.MainClass);

        var gameArguments = version.Arguments?.Game is { Count: > 0 }
            ? ExpandArguments(version.Arguments.Game, context, variables)
            : version.MinecraftArguments is { Length: > 0 } legacyArguments
                ? ArgumentSubstitutor.ExpandAll(ArgumentTokenizer.Split(legacyArguments), variables)
                : throw new InvalidOperationException(
                    $"Version '{version.Id}' has no game arguments.");

        var arguments = new List<string>(jvmArguments.Count + gameArguments.Count);
        arguments.AddRange(jvmArguments);
        arguments.AddRange(gameArguments);

        return new LaunchCommand(
            options.JavaPath,
            arguments,
            options.GameDirectory);
    }

    private static List<string> ExpandArguments(
        IReadOnlyList<VersionArgument> source,
        RuleContext context,
        IReadOnlyDictionary<string, string> variables)
    {
        var result = new List<string>();
        foreach (var argument in source)
        {
            if (!RuleEvaluator.IsAllowed(argument.Rules, context))
            {
                continue;
            }

            foreach (var value in argument.Values)
            {
                result.Add(ArgumentSubstitutor.Expand(value, variables));
            }
        }

        return result;
    }

    private static Dictionary<string, bool> CreateFeatures(LaunchOptions options)
    {
        var features = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (RuleContext.Current.Features is { } defaults)
        {
            foreach (var (key, value) in defaults)
            {
                features[key] = value;
            }
        }

        if (options.Features is { } overrides)
        {
            foreach (var (key, value) in overrides)
            {
                features[key] = value;
            }
        }

        if (options.WindowWidth is not null && options.WindowHeight is not null)
        {
            features["has_custom_resolution"] = true;
        }

        return features;
    }

    private static void AddJvmDefaults(
        List<string> arguments,
        LaunchOptions options,
        string classpath)
    {
        if (!HasClasspathArgument(arguments))
        {
            arguments.Add("-cp");
            arguments.Add(classpath);
        }

        if (!arguments.Any(
                argument => argument.StartsWith("-Djava.library.path=", StringComparison.Ordinal)))
        {
            arguments.Add($"-Djava.library.path={options.NativesDirectory}");
        }

        if (options.MaxMemoryMb > 0 &&
            !arguments.Any(argument => argument.StartsWith("-Xmx", StringComparison.Ordinal)))
        {
            arguments.Add($"-Xmx{options.MaxMemoryMb}M");
        }

        if (options.MinMemoryMb is { } minMemory &&
            minMemory > 0 &&
            !arguments.Any(argument => argument.StartsWith("-Xms", StringComparison.Ordinal)))
        {
            arguments.Add($"-Xms{minMemory}M");
        }

        foreach (var extraArgument in options.ExtraJvmArguments)
        {
            arguments.Add(extraArgument);
        }
    }

    private static void AddLogging(
        List<string> arguments,
        VersionDetail version,
        LaunchOptions options)
    {
        if (options.LoggingConfigurationPath is not { Length: > 0 } path ||
            version.Logging?.Client?.Argument is not { Length: > 0 } loggingArgument)
        {
            return;
        }

        arguments.Add(ArgumentSubstitutor.Expand(
            loggingArgument,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["path"] = path,
            }));
    }

    private static bool HasClasspathArgument(IReadOnlyList<string> arguments) =>
        arguments.Any(argument =>
            string.Equals(argument, "-cp", StringComparison.Ordinal) ||
            string.Equals(argument, "-classpath", StringComparison.Ordinal) ||
            argument.StartsWith("-cp=", StringComparison.Ordinal));

    private static string BuildClasspath(
        VersionDetail version,
        LaunchOptions options,
        RuleContext context)
    {
        var entries = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.ClientJarPath))
        {
            entries.Add(options.ClientJarPath);
        }

        foreach (var library in version.Libraries)
        {
            if (!RuleEvaluator.IsAllowed(library.Rules, context) ||
                LibraryFileResolver.ResolveArtifact(library) is not { } file)
            {
                continue;
            }

            entries.Add(PathSafety.CombineUnderRoot(
                options.LibrariesDirectory,
                file.RelativePath));
        }

        return string.Join(Path.PathSeparator, entries);
    }

    private static Dictionary<string, string> CreateVariables(
        VersionDetail version,
        LaunchOptions options,
        string classpath)
    {
        var account = options.Account;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_player_name"] = account.Name,
            ["version_name"] = options.VersionName.Length > 0 ? options.VersionName : version.Id,
            ["game_directory"] = options.GameDirectory,
            ["assets_root"] = options.AssetsDirectory,
            ["game_assets"] = Path.Combine(options.AssetsDirectory, "virtual", "legacy"),
            ["assets_index_name"] = options.AssetIndexName
                ?? version.AssetIndex?.Id
                ?? version.Assets
                ?? "legacy",
            ["auth_uuid"] = account.Uuid,
            ["auth_access_token"] = account.AccessToken ?? "0",
            ["auth_session"] = $"{account.AccessToken ?? "0"}:{account.Uuid}",
            ["clientid"] = "",
            ["auth_xuid"] = "",
            ["user_type"] = account.Type == AccountType.Microsoft ? "msa" : "legacy",
            ["version_type"] = version.Type,
            ["natives_directory"] = options.NativesDirectory,
            ["launcher_name"] = options.LauncherName,
            ["launcher_version"] = options.LauncherVersion,
            ["classpath"] = classpath,
            ["resolution_width"] = options.WindowWidth?.ToString() ?? "",
            ["resolution_height"] = options.WindowHeight?.ToString() ?? "",
        };
    }
}
