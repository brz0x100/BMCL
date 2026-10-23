using System.Text.RegularExpressions;

namespace BMCL.Core.Minecraft;

public static class ArgumentSubstitutor
{
    private static readonly Regex PlaceholderPattern = new(
        @"\$\{(?<name>[^}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static string Expand(string argument, IReadOnlyDictionary<string, string> variables)
    {
        if (!argument.Contains("${", StringComparison.Ordinal))
        {
            return argument;
        }

        return PlaceholderPattern.Replace(argument, match =>
        {
            var name = match.Groups["name"].Value;
            return variables.TryGetValue(name, out var value) ? value : match.Value;
        });
    }

    public static IReadOnlyList<string> ExpandAll(
        IEnumerable<string> arguments,
        IReadOnlyDictionary<string, string> variables)
    {
        var expanded = new List<string>();
        foreach (var argument in arguments)
        {
            expanded.Add(Expand(argument, variables));
        }

        return expanded;
    }
}
