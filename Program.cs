using System;
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

                var bot1 = new Bot(_configuration, redis, "k1notv");
                var bot2 = new Bot(_configuration, redis, "professorgilbertos");

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
