using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using twitchBot.Utils;

namespace twitchBot
{
    class Program
    {
        private static IConfigurationRoot _configuration;

        static void Main(string[] args)
        {
            try
            {   
                // Create service collection
                Console.WriteLine("Creating service collection");
                ServiceCollection serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                Bot bot = new Bot(_configuration);
                Console.ReadLine();

                //var client = new ChatBotClient("irc.twitch.tv", 6667, "kinobotz", "oauth:retkq3zlim0r5cgf11v8ipk6su3edw", "k1notv");

                //// Ping the server to prevent twitch disconnect your bot
                //var botAlive = new Ping(client);
                //botAlive.Start();

                //// Initial connection
                //Interpreter.Init(client.ReadChatMessage());

                //BasicCommands commands = new BasicCommands();

                //// Message listener
                //while (true)
                //{
                //    var message = client.ReadChatMessage();
                //    var msg = Interpreter.Chat(message);

                //    // listen to commands and respond
                //    var response = commands.CommandListener(msg);
                //    client.SendChatMessages(response);
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(_configuration);
        }
    }
}
