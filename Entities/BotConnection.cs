namespace Entities
{
    public class BotConnection
    {
        public Guid Id { get; set; }
        public string? ChannelId { get; set; }
        public string? Login { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Active { get; set; }
        public string? DiscordClipsWebhookUrl { get; set; }
        public string? DiscordTtsWebhookUrl{ get; set; }
        public ICollection<Command>? ChannelCommands { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Email { get; set; }
        public string? ElevenLabsSimilarityBoost { get; set; }
        public string? ElevenLabsStability { get; set; }
        public string? ElevenLabsApiKey { get; set; }
        public string? ElevenLabsDefaultVoice { get; set; }
        public bool UseTtsOnBits { get; set; }
        public decimal TtsMinimumBitAmount { get; set; }
        public bool UseTtsOnSubscription { get; set; }
        public decimal TtsMinimumResubMonthsAmount { get; set; }
        public UserAccessLevelEnum AccessLevel { get; set; } = UserAccessLevelEnum.Default;
    }
}
