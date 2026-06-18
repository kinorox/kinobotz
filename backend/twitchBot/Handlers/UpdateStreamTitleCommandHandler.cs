using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace twitchBot.Handlers
{
    public class UpdateStreamTitleCommandHandler : BaseCommandHandler<UpdateStreamTitleCommmand>
    {
        public UpdateStreamTitleCommandHandler(IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
        }

        public override async Task<Response> InternalHandle(UpdateStreamTitleCommmand request, CancellationToken cancellationToken)
        {
            if (request.Title.Length > 140)
            {
                return new Response()
                {
                    Message = "Stream title must be less than 140 characters.",
                    WasExecuted = false
                };
            }

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
