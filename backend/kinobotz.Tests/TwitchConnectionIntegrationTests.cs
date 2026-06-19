using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace kinobotz.Tests;

// Live integration tests: connect to Twitch IRC with the bot credentials and verify the
// bot can join and (visibly) send. Gated on KINOBOTZ_BOT_TOKEN so CI without secrets stays
// green. Uses the bot's OWN channel (#kinobotz) so sending a test message is safe.
// Run locally:
//   KINOBOTZ_BOT_TOKEN=<token> KINOBOTZ_BOT_USERNAME=kinobotz dotnet test --filter TwitchConnection
public class TwitchConnectionIntegrationTests
{
    private const string Channel = "kinobotz"; // the bot's own channel

    [Fact]
    public async Task Bot_can_connect_join_and_send_message()
    {
        var token = Environment.GetEnvironmentVariable("KINOBOTZ_BOT_TOKEN");
        if (string.IsNullOrEmpty(token)) return; // gated: no creds (e.g. CI) -> nothing to assert
        var username = Environment.GetEnvironmentVariable("KINOBOTZ_BOT_USERNAME") ?? "kinobotz";

        var client = new TwitchClient(new WebSocketClient(new ClientOptions()));
        var joined = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = $"kinobotz integration test - connection OK ({DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC)";

        client.OnJoinedChannel += (_, e) =>
        {
            joined.TrySetResult(e.Channel);
            client.SendMessage(Channel, message); // visible proof in #kinobotz chat
        };
        client.OnMessageSent += (_, e) => sent.TrySetResult(e.SentMessage.Message);
        client.OnConnectionError += (_, e) => failure.TrySetResult($"connection error: {e.Error?.Message}");
        client.OnIncorrectLogin += (_, e) => failure.TrySetResult($"incorrect login: {e.Exception?.Message}");

        client.Initialize(new ConnectionCredentials(username, token), Channel);
        client.Connect();

        await Task.WhenAny(sent.Task, failure.Task, Task.Delay(TimeSpan.FromSeconds(25)));
        await Task.Delay(2000); // give the message time to appear in chat
        try { client.Disconnect(); } catch { /* ignore */ }

        if (failure.Task.IsCompletedSuccessfully)
        {
            Assert.Fail($"Bot could not connect as '{username}': {failure.Task.Result}");
        }

        Assert.True(joined.Task.IsCompletedSuccessfully, $"Did not receive a JOIN confirmation for #{Channel}.");
        Assert.True(sent.Task.IsCompletedSuccessfully, $"Joined #{Channel} but failed to send a message (send path broken).");
    }

    // Replicates the worker's pattern: one TwitchClient PER channel (N concurrent connections
    // as the same account) to diagnose whether concurrent-connection limits drop joins.
    [Fact]
    public async Task Bot_multiple_concurrent_connections_all_join()
    {
        var token = Environment.GetEnvironmentVariable("KINOBOTZ_BOT_TOKEN");
        if (string.IsNullOrEmpty(token)) return;
        var username = Environment.GetEnvironmentVariable("KINOBOTZ_BOT_USERNAME") ?? "kinobotz";

        var channels = new[]
        {
            "marciosaales", "vilelandre", "splintergprz", "xandghost", "elrikizin",
            "themalkavianx", "shemyazah", "slimclic", "zeszin", "k1notv"
        };

        var joined = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();
        var clients = new List<TwitchClient>();

        foreach (var ch in channels)
        {
            var client = new TwitchClient(new WebSocketClient(new ClientOptions()));
            client.OnJoinedChannel += (_, e) => joined[e.Channel.ToLowerInvariant()] = true;
            client.Initialize(new ConnectionCredentials(username, token), ch);
            client.Connect();
            clients.Add(client);
        }

        await Task.Delay(TimeSpan.FromSeconds(30));
        foreach (var c in clients) { try { c.Disconnect(); } catch { /* ignore */ } }

        var joinedList = string.Join(", ", joined.Keys.OrderBy(x => x));
        var missing = channels.Where(c => !joined.ContainsKey(c.ToLowerInvariant())).ToList();

        Assert.True(missing.Count == 0,
            $"Joined {joined.Count}/{channels.Length}. JOINED: [{joinedList}]. MISSING: [{string.Join(", ", missing)}]");
    }
}
