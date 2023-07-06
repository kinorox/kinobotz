using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class BotConnection
    {
        public Guid Id { get; set; }

        [Required]
        public string ChannelId { get; set; }

        [Required]
        public string Login { get; set; }

        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Active { get; set; }

        public string DiscordClipsWebhookUrl { get; set; }
        public string DiscordTtsWebhookUrl{ get; set; }
    }
}
