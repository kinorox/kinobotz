using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace twitchBot.Commands
{
    public static class CommandHandlerServiceCollectionExtensions
    {
        /// <summary>Registers every ICommandHandler&lt;T&gt; in the assembly plus the dispatcher.</summary>
        public static IServiceCollection AddCommandHandlers(this IServiceCollection services, Assembly assembly)
        {
            var handlerInterface = typeof(ICommandHandler<>);

            var registrations =
                from type in assembly.GetTypes()
                where type is { IsAbstract: false, IsInterface: false }
                from iface in type.GetInterfaces()
                where iface.IsGenericType && iface.GetGenericTypeDefinition() == handlerInterface
                select (iface, type);

            foreach (var (iface, type) in registrations)
            {
                services.AddTransient(iface, type);
            }

            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
            return services;
        }
    }
}
