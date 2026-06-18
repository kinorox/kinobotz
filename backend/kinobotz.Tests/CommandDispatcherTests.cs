using Entities;
using Microsoft.Extensions.DependencyInjection;
using twitchBot.Commands;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;
using RewardRedeemed = TwitchLib.PubSub.Models.Responses.Messages.Redemption.RewardRedeemed;

namespace kinobotz.Tests;

// Locks the MediatR-replacement: the dispatcher must resolve the handler registered
// for a command's RUNTIME type and invoke it (the behavior IMediator.Send provided).
public class CommandDispatcherTests
{
    private sealed class FakeCommand : ICommand
    {
        public string Prefix => "fake";
        public void Build(ChatMessage chatMessage, string command, string commandContent) { }
        public void Build(RewardRedeemed rewardRedeemed) { }
    }

    private sealed class FakeCommandHandler : ICommandHandler<FakeCommand>
    {
        public int Calls { get; private set; }

        public Task<Response> Handle(FakeCommand command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new Response { Message = "handled", WasExecuted = true });
        }
    }

    [Fact]
    public async Task Send_routes_to_the_handler_registered_for_the_runtime_type()
    {
        var handler = new FakeCommandHandler();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<FakeCommand>>(handler);
        var dispatcher = new CommandDispatcher(services.BuildServiceProvider());

        var response = await dispatcher.Send(new FakeCommand());

        Assert.Equal("handled", response.Message);
        Assert.Equal(1, handler.Calls);
    }
}
