using System;
using Microsoft.Extensions.Configuration;
using twitchBot.Utils;
using System.Linq;

namespace twitchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Getting the secret to be used in the password section
                var builder = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();

                //var serviceProvider = builder.Providers.First();
                //if (!serviceProvider.TryGet("TwitchAccess:username", "kinobotz")) return;
                //if (!serviceProvider.TryGet("TwitchAccess:OAuth", out var password)) return;
                //if (!serviceProvider.TryGet("TwitchAccess:channel", out var channel)) return;

                var client = new TwitchClient("irc.twitch.tv", 6667, "kinobotz", "oauth:retkq3zlim0r5cgf11v8ipk6su3edw", "k1notv");

                // Ping the server to prevent twitch disconnect your bot
                var botAlive = new Ping(client);
                botAlive.Start();

                // Initial connection
                Interpreter.Init(client.ReadChatMessage());

                BasicCommands commands = new BasicCommands();

                // Message listener
                while (true)
                {
                    var message = client.ReadChatMessage();
                    var msg = Interpreter.Chat(message);

                    // listen to commands and respond
                    var response = commands.CommandListener(msg);
                    client.SendChatMessages(response);
                }

                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
