using System.Text.Json;
using BMCL.Core.Minecraft;
using Xunit;

namespace BMCL.Tests;

public sealed class ArgumentSubstitutorTests
{
    [Fact]
    public void ReplacesKnownPlaceholders()
    {
        var variables = new Dictionary<string, string>
        {
            ["auth_player_name"] = "Steve",
            ["auth_uuid"] = "5627dd98e6be3c21b8a8e92344183641",
        };

        Assert.Equal(
            "--username Steve --uuid 5627dd98e6be3c21b8a8e92344183641",
            ArgumentSubstitutor.Expand(
                "--username ${auth_player_name} --uuid ${auth_uuid}",
                variables));
    }

    [Fact]
    public void LeavesUnknownPlaceholdersUntouched()
    {
        var variables = new Dictionary<string, string> { ["known"] = "value" };

        Assert.Equal(
            "value ${unknown}",
            ArgumentSubstitutor.Expand("${known} ${unknown}", variables));
    }

    [Fact]
    public void ExpandsEveryArgument()
    {
        var variables = new Dictionary<string, string> { ["name"] = "Alex" };

        var expanded = ArgumentSubstitutor.ExpandAll(
            ["--username", "${name}"],
            variables);

        Assert.Equal(new[] { "--username", "Alex" }, expanded);
    }

    [Fact]
    public void DeserializesStringAndConditionalArguments()
    {
        const string json = """
            {
              "game": [
                "--username",
                "${auth_player_name}",
                {
                  "rules": [
                    { "action": "allow", "features": { "is_demo_user": true } }
                  ],
                  "value": "--demo"
                }
              ],
              "jvm": []
            }
            """;

        var arguments = JsonSerializer.Deserialize<VersionArguments>(
            json,
            MinecraftJson.Options);

        Assert.NotNull(arguments);
        Assert.Equal(3, arguments.Game.Count);
        Assert.Equal("--username", arguments.Game[0].Values[0]);
        Assert.Empty(arguments.Game[0].Rules);
        Assert.Equal("--demo", arguments.Game[2].Values[0]);
        Assert.Single(arguments.Game[2].Rules);
        Assert.True(arguments.Game[2].Rules[0].Features!["is_demo_user"]);
    }

    [Fact]
    public void DeserializesArgumentValueArrays()
    {
        const string json = """
            {
              "game": [
                {
                  "rules": [{ "action": "allow", "os": { "name": "linux" } }],
                  "value": ["--width", "854"]
                }
              ],
              "jvm": []
            }
            """;

        var arguments = JsonSerializer.Deserialize<VersionArguments>(
            json,
            MinecraftJson.Options);

        Assert.NotNull(arguments);
        Assert.Equal(new[] { "--width", "854" }, arguments.Game[0].Values);
        Assert.Equal("linux", arguments.Game[0].Rules[0].Os!.Name);
    }
}
