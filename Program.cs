using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Interfaces;

namespace twitchBot
{
    class Program
    {
        private static IConfiguration _configuration;
        private static TwitchAPI twitchApi;

        static void Main(string[] args)
        {
            try
            {   
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var channels = _configuration["channels"]?.Split(',');
                
                var bot = serviceProvider.GetService<IBot>();

                bot.JoinChannels(channels);

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
            
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            serviceCollection.AddLogging(config => config.AddSerilog(logger, true));
            serviceCollection.AddSingleton(_configuration);
            var redisConfig = new RedisConfiguration() {ConnectionString = _configuration["redis"]};
            serviceCollection.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig); 
            serviceCollection.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            serviceCollection.AddTransient<IBot, Bot>();

            twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _configuration["client_id"],
                    Secret = _configuration["client_secret"]
                }
            };

            twitchApi.Settings.AccessToken = twitchApi.Auth.GetAccessTokenAsync().Result;
            
            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedAccessToken;
            aTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
            aTimer.Enabled = true;

            serviceCollection.AddSingleton<ITwitchAPI>(twitchApi);
        }

        private static void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            twitchApi.Settings.AccessToken = twitchApi.Auth.GetAccessTokenAsync().Result;
        }
    }
}
