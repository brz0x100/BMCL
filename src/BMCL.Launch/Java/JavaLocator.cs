using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BMCL.Launch.Java;

public sealed class JavaLocator : IJavaLocator
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);
    private static readonly char[] VersionSeparators = ['.', '_', '-', '+'];

    public async Task<IReadOnlyList<JavaRuntime>> FindAsync(
        CancellationToken cancellationToken = default)
    {
        var candidates = EnumerateCandidates()
            .Select(TryGetFullPath)
            .OfType<string>()
            .Distinct(GetPathComparer())
            .ToList();

        var probes = candidates.Select(path => ProbeAsync(path, cancellationToken));
        var runtimes = new List<JavaRuntime>();
        var homes = new HashSet<string>(GetPathComparer());

        foreach (var runtime in await Task.WhenAll(probes))
        {
            if (runtime is null)
            {
                continue;
            }

            var key = string.IsNullOrEmpty(runtime.Home) ? runtime.Path : runtime.Home;
            if (homes.Add(key))
            {
                runtimes.Add(runtime);
            }
        }

        return runtimes
            .OrderByDescending(runtime => runtime.MajorVersion)
            .ThenBy(runtime => runtime.Version, StringComparer.Ordinal)
            .ToList();
    }

    private static string? TryGetFullPath(string path)
    {
        try
        {
            return File.Exists(path) ? Path.GetFullPath(path) : null;
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static async Task<JavaRuntime?> ProbeAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProbeTimeout);

            var startInfo = new ProcessStartInfo(path)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-XshowSettings:properties");
            startInfo.ArgumentList.Add("-version");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            try
            {
                var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);

                var output = string.Concat(await stdout, await stderr);
                return Parse(path, output);
            }
            finally
            {
                TryKill(process);
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return null;
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or NotSupportedException or Win32Exception)
        {
        }
    }

    private static JavaRuntime? Parse(string path, string output)
    {
        var version = ExtractProperty(output, "java.version");
        if (version is null)
        {
            return null;
        }

        var home = ExtractProperty(output, "java.home")
            ?? Path.GetDirectoryName(Path.GetDirectoryName(path))
            ?? "";

        return new JavaRuntime(
            path,
            home,
            version,
            ParseMajorVersion(version),
            NormalizeArchitecture(ExtractProperty(output, "os.arch") ?? "unknown"),
            ExtractProperty(output, "java.vendor"));
    }

    private static string NormalizeArchitecture(string architecture) =>
        architecture.ToLowerInvariant() switch
        {
            "amd64" or "x86_64" or "x64" => "x86_64",
            "x86" or "i386" or "i486" or "i586" or "i686" => "x86",
            "aarch64" or "arm64" => "arm64",
            "arm" => "arm",
            var other => other,
        };

    private static string? ExtractProperty(string output, string name)
    {
        var match = Regex.Match(
            output,
            $@"(?m)^\s*{Regex.Escape(name)}\s*=\s*(?<value>.+?)\s*$");
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static int ParseMajorVersion(string version)
    {
        if (version.StartsWith("1.", StringComparison.Ordinal))
        {
            var rest = version[2..];
            var separator = rest.IndexOfAny(VersionSeparators);
            var legacy = separator >= 0 ? rest[..separator] : rest;
            return int.TryParse(legacy, out var major) ? major : 0;
        }

        var index = version.IndexOfAny(VersionSeparators);
        return int.TryParse(index >= 0 ? version[..index] : version, out var result) ? result : 0;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var executable = OperatingSystem.IsWindows() ? "java.exe" : "java";

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            yield return Path.Combine(javaHome.Trim().Trim('"'), "bin", executable);
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathVariable))
        {
            foreach (var directory in pathVariable.Split(
                         Path.PathSeparator,
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return Path.Combine(directory.Trim('"'), executable);
            }
        }

        foreach (var root in GetInstallRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(root);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                yield return OperatingSystem.IsMacOS()
                    ? Path.Combine(directory, "Contents", "Home", "bin", executable)
                    : Path.Combine(directory, "bin", executable);
            }
        }
    }

    private static IEnumerable<string> GetInstallRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            yield return Path.Combine(programFiles, "Java");
            yield return Path.Combine(programFiles, "Eclipse Adoptium");
            yield return Path.Combine(programFiles, "Eclipse Foundation");
            yield return Path.Combine(programFiles, "AdoptOpenJDK");
            yield return Path.Combine(programFiles, "Microsoft");
            yield return Path.Combine(programFiles, "Zulu");
            yield return Path.Combine(programFiles, "BellSoft");
            yield return Path.Combine(programFiles, "Amazon Corretto");
            yield return Path.Combine(localAppData, "Programs", "Eclipse Adoptium");
            yield return Path.Combine(localAppData, "Programs", "Microsoft");
            yield break;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS())
        {
            yield return "/Library/Java/JavaVirtualMachines";
            yield return Path.Combine(userProfile, "Library", "Java", "JavaVirtualMachines");
            yield break;
        }

        yield return "/usr/lib/jvm";
        yield return "/usr/java";
        yield return "/opt/java";
        yield return Path.Combine(userProfile, ".jdks");
        yield return Path.Combine(userProfile, ".sdkman", "candidates", "java");
    }

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
