using System.ClientModel;
using Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace kinobotz.Tests;

// Verifies the GPT quota/auth circuit-breaker: when the provider is out of quota the
// service reports Unavailable, opens the circuit (so it stops hammering the API / spamming
// chat), and re-probes only after the cooldown elapses.
public class GptChatServiceTests
{
    private sealed class ThrowingChatClient : IChatClient
    {
        private readonly Exception _toThrow;
        public int Calls { get; private set; }

        public ThrowingChatClient(Exception toThrow) => _toThrow = toThrow;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw _toThrow;
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    [Fact]
    public async Task Quota_error_reports_unavailable_opens_circuit_then_reprobes_after_cooldown()
    {
        var quota = new ClientResultException("You exceeded your current quota (insufficient_quota).");
        var client = new ThrowingChatClient(quota);
        var time = new FakeTimeProvider();
        var service = new GptChatService(client, NullLogger<GptChatService>.Instance, time, TimeSpan.FromMinutes(5));

        var first = await service.CompleteAsync(new[] { "system" }, "viewer", "hi");
        Assert.True(first.Unavailable);
        Assert.Equal(1, client.Calls);

        // Circuit open: the next call short-circuits without hitting the provider.
        var second = await service.CompleteAsync(new[] { "system" }, "viewer", "hi");
        Assert.True(second.Unavailable);
        Assert.Equal(1, client.Calls);

        // After the cooldown the circuit closes and the provider is queried again.
        time.Advance(TimeSpan.FromMinutes(6));
        var third = await service.CompleteAsync(new[] { "system" }, "viewer", "hi");
        Assert.True(third.Unavailable);
        Assert.Equal(2, client.Calls);
    }
}
