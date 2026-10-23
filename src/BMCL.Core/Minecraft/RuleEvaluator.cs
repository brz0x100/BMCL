using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BMCL.Core.Minecraft;

public static class RuleEvaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static bool IsAllowed(IReadOnlyList<Rule>? rules, RuleContext context)
    {
        if (rules is null || rules.Count == 0)
        {
            return true;
        }

        var allowed = false;
        foreach (var rule in rules)
        {
            if (!Matches(rule, context))
            {
                continue;
            }

            allowed = string.Equals(rule.Action, "allow", StringComparison.OrdinalIgnoreCase);
        }

        return allowed;
    }

    public static string GetCurrentOsName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        return OperatingSystem.IsMacOS() ? "osx" : "linux";
    }

    public static string GetCurrentOsArch() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x86_64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
        };

    private static bool Matches(Rule rule, RuleContext context)
    {
        if (rule.Os is { } os)
        {
            if (os.Name is { Length: > 0 } name &&
                !string.Equals(name, context.OsName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (os.Arch is { Length: > 0 } arch &&
                !string.Equals(arch, context.OsArch, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (os.Version is { Length: > 0 } version &&
                (context.OsVersion is null || !RegexMatches(version, context.OsVersion)))
            {
                return false;
            }
        }

        if (rule.Features is { Count: > 0 } features)
        {
            foreach (var (key, expected) in features)
            {
                var actual = context.Features?.GetValueOrDefault(key) ?? false;
                if (actual != expected)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool RegexMatches(string pattern, string input)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
