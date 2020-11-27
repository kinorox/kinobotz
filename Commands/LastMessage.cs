using System;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Entities;
using twitchBot.Extensions;
using twitchBot.Interfaces;
using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class LastMessage : ICommand
    {
        public string Name => "lm";

        private readonly IRedisCacheClient _redisCacheClient;

        public LastMessage(IRedisCacheClient redisCacheClient)
        {
            _redisCacheClient = redisCacheClient;
        }

        public string Execute(ChatMessage message, string command)
        {
            if (string.Equals(command, "kinobotz"))
            {
                return $"{message.Username} acha mesmo que vou te falar? B)";
            }

            var userName = command.Replace("@", string.Empty).ToLower();

            var userLastMessage =
                _redisCacheClient.Db0.GetAsync<SimplifiedChatMessage>($"{message.Channel}:lastmessage:{userName}");

            var result = userLastMessage.Result;

            if (result == null)
            {
                return $"Não encontrei nenhuma mensagem do usuário @{userName} TearGlove";
            }

            return $"A última mensagem do @{result.UserName} foi ' {result.Message} ' {result.TmiSentTs.ConvertTimestampToDateTime()}' EZ";
        }
    }
}
