using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;
using TwitchLib.Client.Enums;

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

        public Dictionary<string, bool> Commands { get; set; } = new()
        {
            { Entities.Commands.RANDOM_STREAM_TITLE, true },
            { Entities.Commands.UPDATE_STREAM_TITLE, true },
            { Entities.Commands.COMMAND, true },
            { Entities.Commands.EXISTING_COMMANDS, true },
            { Entities.Commands.LAST_MESSAGE, true },
            { Entities.Commands.FIRST_FOLLOW, true },
            { Entities.Commands.CREATE_CLIP, true },
            { Entities.Commands.GPT, true },
            { Entities.Commands.GPT_BEHAVIOR, true },
            { Entities.Commands.GPT_BEHAVIOR_DEFINITION, true },
            { Entities.Commands.TTS, false },
            { Entities.Commands.NOTIFY, true },
            { Entities.Commands.ENABLE, true },
            { Entities.Commands.DISABLE, true }
        };

        public string ProfileImageUrl { get; set; }
        public string Email { get; set; }
        public UserAccessLevelEnum AccessLevel { get; set; } = UserAccessLevelEnum.Default;
    }
}
