using System;
using System.IO;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using twitchBot.Utils;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using ILogger = Serilog.ILogger;

namespace twitchBot
{
    class Program
    {
        private static IConfiguration _configuration;
        private static TwitchAPI twitchApi;

        static void Main(string[] args)
        {
            ILogger<Program> Logger = null;

            try
            {
                DotEnv.Load();

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var channels = _configuration["channels"]?.Split(',');
                
                var bot = serviceProvider.GetService<IBot>();

                Logger = serviceProvider.GetService<ILogger<Program>>();

                bot.JoinChannels(channels);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                if (Logger != null) Logger.LogError(e, e.Message);
                else Console.WriteLine(e.Message);
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();
            
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            serviceCollection.AddLogging(config => config.AddSerilog(logger, true));
            serviceCollection.AddSingleton(_configuration);
            var redisConfig = new RedisConfiguration() {ConnectionString = $"{_configuration["redis_host"]},password={_configuration["redis_password"]},allowAdmin=true"};
            serviceCollection.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig); 
            serviceCollection.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            serviceCollection.AddTransient<IBot, Bot>();

            twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _configuration["client_id"],
                    Secret = _configuration["client_secret"],
                    AccessToken = _configuration["access_token"]
                }
            };

            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedAccessToken;
            aTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
            aTimer.Enabled = true;

            serviceCollection.AddSingleton<ITwitchAPI>(twitchApi);
        }

        private static void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            twitchApi.Auth.RefreshAuthTokenAsync(twitchApi.Settings.AccessToken, _configuration["client_secret"], _configuration["client_id"]);
        }
    }
}
