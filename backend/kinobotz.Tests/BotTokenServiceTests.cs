using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace kinobotz.Tests;

// Verifies the bot chat token lifecycle: seeded from config, refreshed before it expires,
// persisted, re-seeded when the config refresh token changes, and forced refreshes debounced.
public class BotTokenServiceTests
{
    private sealed class InMemoryStore : IBotTokenStore
    {
        public BotToken? Token { get; set; }
        public int Saves { get; private set; }

        public Task<BotToken?> Get() => Task.FromResult(Token);

        public Task Save(BotToken token)
        {
            Saves++;
            Token = token;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefresher : ITwitchTokenRefresher
    {
        public List<string> Calls { get; } = new();
        public bool Fail { get; set; }

        public Task<RefreshedToken> Refresh(string refreshToken)
        {
            Calls.Add(refreshToken);
            if (Fail) throw new InvalidOperationException("refresh failed");
            var n = Calls.Count;
            return Task.FromResult(new RefreshedToken($"access-{n}", $"refresh-{n}", 4 * 3600));
        }
    }

    private static IConfiguration Config(string? access, string? refresh) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["bot_access_token"] = access,
                ["bot_refresh_token"] = refresh
            })
            .Build();

    private static BotTokenService Service(InMemoryStore store, FakeRefresher refresher, IConfiguration config, FakeTimeProvider time) =>
        new(store, refresher, config, NullLogger<BotTokenService>.Instance, time);

    [Fact]
    public async Task Without_refresh_token_returns_config_token_and_never_refreshes()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var service = Service(store, refresher, Config("config-access", null), new FakeTimeProvider());

        Assert.Equal("config-access", await service.GetAccessToken());
        Assert.Empty(refresher.Calls);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task Seeds_from_config_refreshes_and_persists()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var service = Service(store, refresher, Config("config-access", "seed-refresh"), new FakeTimeProvider());

        Assert.Equal("access-1", await service.GetAccessToken());
        Assert.Equal(new[] { "seed-refresh" }, refresher.Calls);
        Assert.Equal("refresh-1", store.Token!.RefreshToken);
        Assert.NotNull(store.Token.Seed);
    }

    [Fact]
    public async Task Refreshes_only_when_close_to_expiry()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var time = new FakeTimeProvider();
        var service = Service(store, refresher, Config("config-access", "seed-refresh"), time);

        await service.GetAccessToken();
        time.Advance(TimeSpan.FromHours(3));
        Assert.Equal("access-1", await service.GetAccessToken());

        // within the 15 minute margin of the 4 hour expiry
        time.Advance(TimeSpan.FromMinutes(50));
        Assert.Equal("access-2", await service.GetAccessToken());
        Assert.Equal(new[] { "seed-refresh", "refresh-1" }, refresher.Calls);
    }

    [Fact]
    public async Task Uses_stored_token_after_restart_without_reseeding()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var time = new FakeTimeProvider();
        var config = Config("config-access", "seed-refresh");
        await Service(store, refresher, config, time).GetAccessToken();

        // a new process (dyno restart) with the same config
        var restarted = Service(store, refresher, config, time);
        Assert.Equal("access-1", await restarted.GetAccessToken());
        Assert.Single(refresher.Calls);
    }

    [Fact]
    public async Task Reseeds_when_config_refresh_token_changes()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var time = new FakeTimeProvider();
        await Service(store, refresher, Config("config-access", "seed-refresh"), time).GetAccessToken();

        var reseeded = Service(store, refresher, Config("new-access", "new-seed"), time);
        Assert.Equal("access-2", await reseeded.GetAccessToken());
        Assert.Equal(new[] { "seed-refresh", "new-seed" }, refresher.Calls);
    }

    [Fact]
    public async Task Forced_refresh_is_debounced()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher();
        var time = new FakeTimeProvider();
        var service = Service(store, refresher, Config("config-access", "seed-refresh"), time);

        await service.GetAccessToken();
        Assert.Equal("access-1", await service.GetAccessToken(forceRefresh: true));

        time.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal("access-2", await service.GetAccessToken(forceRefresh: true));
    }

    [Fact]
    public async Task Failed_refresh_keeps_current_token()
    {
        var store = new InMemoryStore();
        var refresher = new FakeRefresher { Fail = true };
        var service = Service(store, refresher, Config("config-access", "seed-refresh"), new FakeTimeProvider());

        Assert.Equal("config-access", await service.GetAccessToken());
        Assert.Equal(0, store.Saves);
    }
}
