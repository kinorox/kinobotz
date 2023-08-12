using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace twitchBot.Handlers
{
    public class UpdateStreamTitleCommandHandler : BaseCommandHandler<UpdateStreamTitleCommmand>
    {
        public UpdateStreamTitleCommandHandler(IRedisClient redisClient, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(redisClient, botConnectionRepository, configuration)
        {
        }

        public override async Task<Response> InternalHandle(UpdateStreamTitleCommmand request, CancellationToken cancellationToken)
        {
            await TwitchApi.Helix.Channels.ModifyChannelInformationAsync(request.BotConnection.ChannelId,
                new ModifyChannelInformationRequest()
                {
                    Title = request.Title
                });

            return new Response()
            {
                Message = "Stream title updated."
            };
        }
    }
}
