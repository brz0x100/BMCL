namespace BMCL.Core.Minecraft;

public sealed record VersionArgument
{
    public VersionArgument(IReadOnlyList<string> values, IReadOnlyList<Rule> rules)
    {
        Values = values;
        Rules = rules;
    }

    public IReadOnlyList<string> Values { get; }

    public IReadOnlyList<Rule> Rules { get; }

    public static VersionArgument FromValue(string value) => new([value], []);
}

public sealed record VersionArguments
{
    public IReadOnlyList<VersionArgument> Game { get; init; } = [];

    public IReadOnlyList<VersionArgument> Jvm { get; init; } = [];
}
