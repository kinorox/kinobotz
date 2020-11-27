using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Interfaces;
using TwitchLib.Api.Core.Models.Undocumented.CSStreams;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Models;
using User = TwitchLib.Api.V5.Models.Users.User;

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

            Users user = null;

            try
            {
                user = Bot.Api.V5.Users.GetUserByNameAsync(userName.ToLower()).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return null;
            }
            
            if (user != null && user.Total > 0)
            {
                var resultUser = user.Matches.FirstOrDefault();

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
