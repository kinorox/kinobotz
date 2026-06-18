using Entities;

namespace webapi.Dto
{
    public static class BotConnectionMappingExtensions
    {
        public static BotConnectionDto ToDto(this BotConnection b) => new()
        {
            Id = b.Id,
            Login = b.Login,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            Active = b.Active,
            DiscordClipsWebhookUrl = b.DiscordClipsWebhookUrl,
            DiscordTtsWebhookUrl = b.DiscordTtsWebhookUrl,
            ChannelCommands = b.ChannelCommands?.ToList() ?? new List<Command>(),
            ElevenLabsSimilarityBoost = b.ElevenLabsSimilarityBoost,
            ElevenLabsStability = b.ElevenLabsStability,
            ElevenLabsApiKey = b.ElevenLabsApiKey,
            ElevenLabsDefaultVoice = b.ElevenLabsDefaultVoice,
            UseTtsOnBits = b.UseTtsOnBits,
            TtsMinimumBitAmount = b.TtsMinimumBitAmount,
            UseTtsOnSubscription = b.UseTtsOnSubscription,
            TtsMinimumResubMonthsAmount = b.TtsMinimumResubMonthsAmount
        };
    }
}
