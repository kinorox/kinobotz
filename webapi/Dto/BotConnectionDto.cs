namespace webapi.Dto
{
    public class BotConnectionDto
    {
        public Guid Id { get; set; }
        public string? Login { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Email { get; set; }
        public bool? Active { get; set; }
        public string? DiscordClipsWebhookUrl { get; set; }
        public string? DiscordTtsWebhookUrl { get; set; }
        public Dictionary<string, bool> Commands { get; set; }
    }
}
