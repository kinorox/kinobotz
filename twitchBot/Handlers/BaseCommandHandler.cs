using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using MediatR;

namespace twitchBot.Handlers
{
    public abstract class BaseCommandHandler<T> : IRequestHandler<T, Response> where T : IRequest<Response>
    {
        public abstract Task<Response> InternalHandle(T request, CancellationToken cancellationToken);

        public async Task<Response> Handle(T request, CancellationToken cancellationToken)
        {
            try
            {
                return await InternalHandle(request, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
