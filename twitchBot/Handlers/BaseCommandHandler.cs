using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using MediatR;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public abstract class BaseCommandHandler<T> : IRequestHandler<T, Response> where T : BaseCommand
    {
        private readonly IRedisClient redisClient;

        protected BaseCommandHandler(IRedisClient redisClient)
        {
            this.redisClient = redisClient;
        }

        public virtual int Cooldown => 0;

        public virtual bool GlobalCooldown => false;

        public abstract Task<Response> InternalHandle(T request, CancellationToken cancellationToken);

        public async Task<Response> Handle(T request, CancellationToken cancellationToken)
        {
            try
            {
                var lastExecutionTime = GlobalCooldown ?
                    await redisClient.Db0.GetAsync<DateTime>($"{request.BotConnection.Id}:{request.Prefix}:lastexecution") :
                    await redisClient.Db0.GetAsync<DateTime>($"{request.BotConnection.Id}:{request.Prefix}:lastexecution:{request.Username}");

                if (lastExecutionTime.AddMinutes(Cooldown) <= DateTime.UtcNow)
                {
                    var response = await InternalHandle(request, cancellationToken);

                    if (!response.WasExecuted) return response;

                    var timeNow = DateTime.UtcNow;

                    await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:lastexecution", timeNow);
                    await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:lastexecution:{request.Username}", timeNow);

                    return response;
                }

                var difference = lastExecutionTime.AddMinutes(Cooldown) - DateTime.UtcNow;

                return new Response()
                {
                    Message = $"Wait {(difference.Minutes == 0 ? difference.Seconds + "s" : difference.Minutes + "min")} to execute this command again."
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
