using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// <see cref="IChatClient"/>-backed chat service with a quota/auth circuit-breaker:
    /// when the provider reports it is out of quota or unauthorized, further calls are
    /// short-circuited for a cooldown window so a dead key doesn't spam chat or burn calls.
    /// </summary>
    public class GptChatService : IGptChatService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<GptChatService> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _circuitCooldown;

        private readonly object _gate = new();
        private DateTimeOffset? _circuitOpenUntil;

        public GptChatService(
            IChatClient chatClient,
            ILogger<GptChatService> logger,
            TimeProvider timeProvider,
            TimeSpan? circuitCooldown = null)
        {
            _chatClient = chatClient;
            _logger = logger;
            _timeProvider = timeProvider;
            _circuitCooldown = circuitCooldown ?? TimeSpan.FromMinutes(5);
        }

        public async Task<GptResult> CompleteAsync(
            IReadOnlyList<string> systemMessages,
            string userName,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_circuitOpenUntil is { } until && _timeProvider.GetUtcNow() < until)
                {
                    return GptResult.Quota();
                }
            }

            var messages = new List<ChatMessage>(systemMessages.Count + 1);
            foreach (var system in systemMessages)
            {
                if (!string.IsNullOrEmpty(system))
                {
                    messages.Add(new ChatMessage(ChatRole.System, system));
                }
            }
            messages.Add(new ChatMessage(ChatRole.User, userMessage) { AuthorName = userName });

            try
            {
                var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
                return GptResult.Ok(response.Text ?? string.Empty);
            }
            catch (ClientResultException ex) when (IsQuotaOrAuth(ex))
            {
                lock (_gate)
                {
                    _circuitOpenUntil = _timeProvider.GetUtcNow().Add(_circuitCooldown);
                }
                _logger.LogError(ex, "GPT unavailable (quota/auth). Circuit opened for {Cooldown}.", _circuitCooldown);
                return GptResult.Quota();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPT request failed.");
                return GptResult.Failure(ex.Message);
            }
        }

        // Detects out-of-quota / unauthorized responses by HTTP status when available,
        // falling back to the error text (e.g. OpenAI's "insufficient_quota").
        private static bool IsQuotaOrAuth(ClientResultException ex)
        {
            int? status = null;
            try { status = ex.Status; } catch { /* exception carries no HTTP response */ }

            if (status is 429 or 401 or 403)
            {
                return true;
            }

            return ex.Message.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("exceeded your current quota", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
        }
    }
}
