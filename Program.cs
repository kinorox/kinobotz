using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                // Create service collection
                Console.WriteLine("Creating service collection");
                ServiceCollection serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                
                var serviceProvider = serviceCollection.BuildServiceProvider();

                var redis = serviceProvider.GetService<IRedisCacheClient>();

                var channels = _configuration["channels"];
                var splittedChannels = channels.Split(";");

                var bots = new List<Bot>();

                foreach (var channel in splittedChannels)
                {
                    var bot = new Bot(_configuration, redis, channel);

                    bots.Add(bot);
                }

                while (true)
                {
                    
                }
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

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton(_configuration);

            var redisConfiguration = _configuration.GetSection("Redis").Get<RedisConfiguration>();

            serviceCollection.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration);
        }
    }
}
