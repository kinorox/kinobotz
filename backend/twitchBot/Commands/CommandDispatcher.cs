using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.DependencyInjection;

namespace twitchBot.Commands
{
    /// <summary>Resolves the handler for a command's runtime type from DI and invokes it.
    /// Replaces MediatR's IMediator.Send.</summary>
    public interface ICommandDispatcher
    {
        Task<Response> Send(ICommand command, CancellationToken cancellationToken = default);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Handle)> Cache = new();

        private readonly IServiceProvider _serviceProvider;

        public CommandDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<Response> Send(ICommand command, CancellationToken cancellationToken = default)
        {
            var (handlerType, handle) = Cache.GetOrAdd(command.GetType(), commandType =>
            {
                var ht = typeof(ICommandHandler<>).MakeGenericType(commandType);
                // Invoke via the public interface method so handler accessibility never matters.
                var method = ht.GetMethod(nameof(ICommandHandler<ICommand>.Handle))!;
                return (ht, method);
            });

            var handler = _serviceProvider.GetRequiredService(handlerType);
            return (Task<Response>)handle.Invoke(handler, new object[] { command, cancellationToken })!;
        }
    }
}
