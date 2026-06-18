using Entities;

namespace webapi.Dto
{
    public class BotConnectionDto
    {
        public Guid Id { get; set; }
        public string? Login { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Active { get; set; }
        public string? DiscordClipsWebhookUrl { get; set; }
        public string? DiscordTtsWebhookUrl { get; set; }
        public List<Command> ChannelCommands { get; set; }
        public string? ElevenLabsSimilarityBoost { get; set; }
        public string? ElevenLabsStability { get; set; }
        public string? ElevenLabsApiKey { get; set; }
        public string? ElevenLabsDefaultVoice { get; set; }
        public bool UseTtsOnBits { get; set; }
        public decimal TtsMinimumBitAmount { get; set; }
        public bool UseTtsOnSubscription { get; set; }
        public decimal TtsMinimumResubMonthsAmount { get; set; }
    }
}
