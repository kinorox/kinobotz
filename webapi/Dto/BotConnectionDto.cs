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
        public string ElevenLabsSimilarityBoost { get; set; }
        public string ElevenLabsStability { get; set; }
    }
}
