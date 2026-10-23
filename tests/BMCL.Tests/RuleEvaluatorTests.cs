using BMCL.Core.Minecraft;
using Xunit;

namespace BMCL.Tests;

public sealed class RuleEvaluatorTests
{
    [Fact]
    public void AllowsEverythingWhenNoRulesArePresent()
    {
        Assert.True(RuleEvaluator.IsAllowed(null, new RuleContext("linux", "x86_64")));
        Assert.True(RuleEvaluator.IsAllowed([], new RuleContext("linux", "x86_64")));
    }

    [Fact]
    public void AllowsOnlyMatchingOperatingSystem()
    {
        IReadOnlyList<Rule> rules =
        [
            new Rule { Action = "allow", Os = new RuleOs { Name = "linux" } },
        ];

        Assert.True(RuleEvaluator.IsAllowed(rules, new RuleContext("linux", "x86_64")));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("windows", "x86_64")));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("osx", "arm64")));
    }

    [Fact]
    public void DisallowRuleOverridesEarlierAllowRule()
    {
        IReadOnlyList<Rule> rules =
        [
            new Rule { Action = "allow" },
            new Rule { Action = "disallow", Os = new RuleOs { Name = "osx" } },
        ];

        Assert.True(RuleEvaluator.IsAllowed(rules, new RuleContext("linux", "x86_64")));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("osx", "arm64")));
    }

    [Fact]
    public void MatchesArchitecture()
    {
        IReadOnlyList<Rule> rules =
        [
            new Rule { Action = "allow", Os = new RuleOs { Arch = "arm64" } },
        ];

        Assert.True(RuleEvaluator.IsAllowed(rules, new RuleContext("osx", "arm64")));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("osx", "x86_64")));
    }

    [Fact]
    public void MatchesOperatingSystemVersionPattern()
    {
        IReadOnlyList<Rule> rules =
        [
            new Rule { Action = "allow", Os = new RuleOs { Name = "windows", Version = "^10\\." } },
        ];

        Assert.True(RuleEvaluator.IsAllowed(rules, new RuleContext("windows", "x86_64", "10.0.22631")));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("windows", "x86_64", "6.1.7601")));
    }

    [Fact]
    public void MatchesFeatureFlags()
    {
        IReadOnlyList<Rule> rules =
        [
            new Rule
            {
                Action = "allow",
                Features = new Dictionary<string, bool> { ["is_demo_user"] = true },
            },
        ];

        var enabled = new RuleContext(
            "linux",
            "x86_64",
            features: new Dictionary<string, bool> { ["is_demo_user"] = true });
        var disabled = new RuleContext(
            "linux",
            "x86_64",
            features: new Dictionary<string, bool> { ["is_demo_user"] = false });

        Assert.True(RuleEvaluator.IsAllowed(rules, enabled));
        Assert.False(RuleEvaluator.IsAllowed(rules, disabled));
        Assert.False(RuleEvaluator.IsAllowed(rules, new RuleContext("linux", "x86_64")));
    }
}
