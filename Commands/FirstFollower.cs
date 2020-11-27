using System.Linq;
using System.Text;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Interfaces;
using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class FirstFollower : ICommand
    {
        private readonly IRedisCacheClient _redisCacheClient;

        public FirstFollower(IRedisCacheClient redisCacheClient)
        {
            _redisCacheClient = redisCacheClient;
        }

        public string Name => "ff";

        public string Execute(ChatMessage message, string command)
        {
            if (string.IsNullOrEmpty(command))
                return null;
            
            var userName = command.Replace("@", string.Empty).ToLower();

            var user = Bot.Api.V5.Users.GetUserByNameAsync(userName.ToLower());

            if (user.Result.Total > 0)
            {
                var resultUser = user.Result.Matches.FirstOrDefault();

                if (resultUser != null)
                {
                    var userFollows = Bot.Api.V5.Users.GetUserFollowsAsync(userId: resultUser.Id, direction: "asc").Result;

                    var firstUserFollow = userFollows.Follows.FirstOrDefault();

                    var userFollowMessage = new StringBuilder();
                    userFollowMessage.Append(firstUserFollow != null
                        ? $"seguiu primeiro @{firstUserFollow.Channel.Name}"
                        : "ainda não seguiu nenhum canal");

                    var channelFollows =
                        Bot.Api.V5.Channels.GetChannelFollowersAsync(channelId: resultUser.Id, direction: "asc").Result;

                    var firstChannelFollow = channelFollows.Follows.FirstOrDefault();

                    var firstChannelFollowedMessage = new StringBuilder();
                    firstChannelFollowedMessage.Append(firstChannelFollow != null
                        ? $"foi seguido primeiro por @{firstChannelFollow.User.Name}"
                        : "ainda não tem followers");

                    return $"@{message.Username}, @{resultUser.Name} {userFollowMessage} e {firstChannelFollowedMessage} SeemsGood";
                }
            }

            return $"Não encontrei o usuário {command.ToLower()} BibleThump";
        }
    }
}
