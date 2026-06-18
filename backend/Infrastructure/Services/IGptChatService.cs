namespace Infrastructure.Services
{
    /// <summary>
    /// Abstraction over the chat-completion provider. Centralizes prompt assembly and
    /// quota/auth hardening so command handlers stay provider-agnostic.
    /// </summary>
    public interface IGptChatService
    {
        Task<GptResult> CompleteAsync(
            IReadOnlyList<string> systemMessages,
            string userName,
            string userMessage,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Outcome of a chat completion. Exactly one of the states holds.</summary>
    public record GptResult
    {
        public string? Text { get; init; }

        /// <summary>Quota/auth failure (e.g. OpenAI insufficient_quota / 401). The circuit is open;
        /// callers should show a friendly "temporarily unavailable" message rather than the raw error.</summary>
        public bool Unavailable { get; init; }

        /// <summary>A transient/other error (not quota). Detail for logging only — not for chat.</summary>
        public string? ErrorMessage { get; init; }

        public static GptResult Ok(string text) => new() { Text = text };
        public static GptResult Quota() => new() { Unavailable = true };
        public static GptResult Failure(string message) => new() { ErrorMessage = message };
    }
}
