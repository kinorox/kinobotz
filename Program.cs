using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace twitchBot
{
    class Program
    {
        private static IConfiguration _configuration;

        static void Main(string[] args)
        {
            try
            {   
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var channels = _configuration.GetSection("channels").Get<List<string>>();

                var connectedChannels = new List<IBot>();

                foreach (var channel in channels)
                {
                    var bot = serviceProvider.GetService<IBot>();

                    bot.Connect(channel);

                    connectedChannels.Add(bot);
                }

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            var redisConfiguration = _configuration.GetSection("Redis").Get<RedisConfiguration>();

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            serviceCollection.AddLogging(config => config.AddSerilog(logger, true));
            serviceCollection.AddSingleton(_configuration);
            serviceCollection.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration); 
            serviceCollection.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            serviceCollection.AddTransient<IBot, Bot>();
        }
    }
}
