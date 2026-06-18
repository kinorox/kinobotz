using Entities;

namespace kinobotz.Tests;

// Characterization test: command gating compares the caller's
// UserAccessLevelEnum against the command's required level and relies on this
// ordinal ordering. Lock it so a future refactor can't reorder the enum and
// silently change who can run privileged commands.
public class AccessLevelOrderingTests
{
    [Fact]
    public void Access_levels_keep_their_expected_ordinal_ordering()
    {
        Assert.Equal(0, (int)UserAccessLevelEnum.Default);
        Assert.Equal(1, (int)UserAccessLevelEnum.Vip);
        Assert.Equal(2, (int)UserAccessLevelEnum.Subscriber);
        Assert.Equal(3, (int)UserAccessLevelEnum.Moderator);
        Assert.Equal(4, (int)UserAccessLevelEnum.Broadcaster);
        Assert.Equal(5, (int)UserAccessLevelEnum.Premium);
        Assert.Equal(6, (int)UserAccessLevelEnum.Admin);
    }

    [Fact]
    public void Broadcaster_outranks_moderator()
    {
        Assert.True(UserAccessLevelEnum.Broadcaster > UserAccessLevelEnum.Moderator);
    }
}
