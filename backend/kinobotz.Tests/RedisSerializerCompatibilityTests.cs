using Entities;
using StackExchange.Redis.Extensions.Newtonsoft;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace kinobotz.Tests;

// Gates the Newtonsoft -> System.Text.Json serializer swap: proves data written by one
// serializer round-trips through the other, so existing prod Redis data (incl. OAuth
// tokens) stays readable after the swap and a rollback stays safe too.
// Pure in-memory — no Redis/Docker needed.
public class RedisSerializerCompatibilityTests
{
    private static readonly NewtonsoftSerializer Newtonsoft = new();
    private static readonly SystemTextJsonSerializer SystemTextJson = new();

    private static BotConnection Sample() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ChannelId = "132711893",
        Login = "k1notv",
        AccessToken = "oauth-access-token-abc",
        RefreshToken = "oauth-refresh-token-xyz",
        CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc),
        Active = true,
        ElevenLabsApiKey = "el-key",
        ElevenLabsDefaultVoice = "Rachel",
        UseTtsOnBits = true,
        TtsMinimumBitAmount = 100m,
        UseTtsOnSubscription = false,
        TtsMinimumResubMonthsAmount = 3m,
        AccessLevel = UserAccessLevelEnum.Broadcaster,
        ChannelCommands = new List<Command>
        {
            new() { Prefix = "tts", AccessLevel = UserAccessLevelEnum.Default, Cooldown = 10, GlobalCooldown = true, Description = "Text to speech", Enabled = true }
        }
    };

    [Fact]
    public void BotConnection_round_trips_newtonsoft_to_systemtextjson()
    {
        var original = Sample();
        var result = SystemTextJson.Deserialize<BotConnection>(Newtonsoft.Serialize(original));
        AssertEquivalent(original, result);
    }

    [Fact]
    public void BotConnection_round_trips_systemtextjson_to_newtonsoft()
    {
        var original = Sample();
        var result = Newtonsoft.Deserialize<BotConnection>(SystemTextJson.Serialize(original));
        AssertEquivalent(original, result);
    }

    [Fact]
    public void DateTime_round_trips_both_ways_preserving_the_instant()
    {
        var dt = new DateTime(2026, 6, 18, 12, 34, 56, DateTimeKind.Utc);
        Assert.Equal(dt.ToUniversalTime(), SystemTextJson.Deserialize<DateTime>(Newtonsoft.Serialize(dt)).ToUniversalTime());
        Assert.Equal(dt.ToUniversalTime(), Newtonsoft.Deserialize<DateTime>(SystemTextJson.Serialize(dt)).ToUniversalTime());
    }

    private static void AssertEquivalent(BotConnection expected, BotConnection actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.ChannelId, actual.ChannelId);
        Assert.Equal(expected.Login, actual.Login);
        Assert.Equal(expected.AccessToken, actual.AccessToken);
        Assert.Equal(expected.RefreshToken, actual.RefreshToken);
        Assert.Equal(expected.Active, actual.Active);
        Assert.Equal(expected.TtsMinimumBitAmount, actual.TtsMinimumBitAmount);
        Assert.Equal(expected.UseTtsOnBits, actual.UseTtsOnBits);
        Assert.Equal(expected.AccessLevel, actual.AccessLevel);

        Assert.NotNull(actual.ChannelCommands);
        Assert.Equal(expected.ChannelCommands!.Count, actual.ChannelCommands!.Count);
        var ec = expected.ChannelCommands!.First();
        var ac = actual.ChannelCommands!.First();
        Assert.Equal(ec.Prefix, ac.Prefix);
        Assert.Equal(ec.Cooldown, ac.Cooldown);
        Assert.Equal(ec.GlobalCooldown, ac.GlobalCooldown);
        Assert.Equal(ec.AccessLevel, ac.AccessLevel);
        Assert.Equal(ec.Enabled, ac.Enabled);
    }
}
