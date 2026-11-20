using Microsoft.Extensions.Logging.Abstractions;
using twitchBot.Handlers;

namespace kinobotz.Tests;

// Verifies %clip waits for Twitch to finish the clip, retries a clip that never shows up,
// and gives up cleanly when the create request itself fails.
public class ClipCreatorTests
{
    private static readonly ClipCreatorOptions FastOptions =
        new(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(100), 2);

    private static ClipCreator NewCreator() => new(TimeProvider.System, NullLogger.Instance, FastOptions);

    [Fact]
    public async Task Returns_url_once_the_clip_finishes_processing()
    {
        var lookups = 0;

        var url = await NewCreator().Create("chan",
            () => Task.FromResult("clip-1"),
            id => Task.FromResult(++lookups < 3 ? null : $"https://clips/{id}"));

        Assert.Equal("https://clips/clip-1", url);
        Assert.Equal(3, lookups);
    }

    [Fact]
    public async Task Requests_a_new_clip_when_the_first_never_shows_up()
    {
        var creates = 0;

        var url = await NewCreator().Create("chan",
            () => Task.FromResult($"clip-{++creates}"),
            id => Task.FromResult(id == "clip-2" ? $"https://clips/{id}" : null));

        Assert.Equal("https://clips/clip-2", url);
        Assert.Equal(2, creates);
    }

    [Fact]
    public async Task Gives_up_after_max_attempts()
    {
        var creates = 0;

        var url = await NewCreator().Create("chan",
            () => Task.FromResult($"clip-{++creates}"),
            _ => Task.FromResult<string>(null));

        Assert.Null(url);
        Assert.Equal(2, creates);
    }

    [Fact]
    public async Task Does_not_retry_when_the_create_request_throws()
    {
        var creates = 0;

        var url = await NewCreator().Create("chan",
            () => { creates++; throw new InvalidOperationException("channel offline"); },
            _ => Task.FromResult<string>("unused"));

        Assert.Null(url);
        Assert.Equal(1, creates);
    }

    [Fact]
    public async Task Keeps_polling_through_transient_lookup_errors()
    {
        var lookups = 0;

        var url = await NewCreator().Create("chan",
            () => Task.FromResult("clip-1"),
            id => ++lookups == 1 ? throw new HttpRequestException("429") : Task.FromResult($"https://clips/{id}"));

        Assert.Equal("https://clips/clip-1", url);
    }

    [Fact]
    public async Task Waits_between_lookups_instead_of_hammering_the_api()
    {
        var lookups = 0;

        await NewCreator().Create("chan",
            () => Task.FromResult("clip-1"),
            _ => { lookups++; return Task.FromResult<string>(null); });

        // 2 attempts x 100ms at a 5ms interval is ~40 lookups at most; a tight loop would do thousands.
        Assert.InRange(lookups, 2, 60);
    }
}
