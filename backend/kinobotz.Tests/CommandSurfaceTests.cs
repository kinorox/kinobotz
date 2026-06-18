using Entities;

namespace kinobotz.Tests;

// Characterization tests: lock the existing default command surface so the
// Phase 3 architecture refactor (MediatR removal) and the Phase 8 docs work
// cannot silently change which commands exist or their access/cooldown rules.
public class CommandSurfaceTests
{
    [Fact]
    public void DefaultCommands_has_the_expected_14_commands()
    {
        Assert.Equal(14, Commands.DefaultCommands.Count);
    }

    [Fact]
    public void DefaultCommands_expose_exactly_the_known_prefixes()
    {
        var expected = new[]
        {
            "disable", "enable", "rtitle", "title", "command", "commands",
            "lm", "ff", "clip", "gpt", "gptbehavior", "gptbehaviordef",
            "tts", "notify"
        };
        var actual = Commands.DefaultCommands.Select(c => c.Prefix);
        Assert.Equal(expected.OrderBy(p => p), actual.OrderBy(p => p));
    }

    [Fact]
    public void All_default_commands_are_enabled()
    {
        Assert.All(Commands.DefaultCommands, c => Assert.True(c.Enabled));
    }

    [Theory]
    [InlineData("disable", UserAccessLevelEnum.Moderator)]
    [InlineData("enable", UserAccessLevelEnum.Moderator)]
    [InlineData("title", UserAccessLevelEnum.Moderator)]
    [InlineData("command", UserAccessLevelEnum.Moderator)]
    [InlineData("rtitle", UserAccessLevelEnum.Broadcaster)]
    [InlineData("gpt", UserAccessLevelEnum.Default)]
    [InlineData("tts", UserAccessLevelEnum.Default)]
    [InlineData("lm", UserAccessLevelEnum.Default)]
    public void Command_access_levels_are_locked(string prefix, UserAccessLevelEnum expected)
    {
        var cmd = Commands.DefaultCommands.Single(c => c.Prefix == prefix);
        Assert.Equal(expected, cmd.AccessLevel);
    }

    [Theory]
    [InlineData("tts")]
    [InlineData("gptbehavior")]
    public void Throttled_commands_keep_their_10_minute_global_cooldown(string prefix)
    {
        var cmd = Commands.DefaultCommands.Single(c => c.Prefix == prefix);
        Assert.Equal(10, cmd.Cooldown);
        Assert.True(cmd.GlobalCooldown);
    }
}
