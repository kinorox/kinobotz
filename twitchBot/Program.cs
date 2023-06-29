using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using ElevenLabs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using Serilog;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using twitchBot.Utils;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using Timer = System.Timers.Timer;

namespace twitchBot
{
    class Program
    {
        private static IConfiguration _configuration;
        private static TwitchAPI _twitchApi;

        static async Task Main(string[] args)
        {
            ILogger<Program> Logger = null;

            try
            {
                DotEnv.Load();

                var builder = new HostBuilder();

                builder.ConfigureServices(ConfigureServices);
                
                var host = builder.Build();

                var serviceProvider = host.Services;

                var channels = _configuration["channels"]?.Split(',');

                var bot = serviceProvider.GetService<IBot>();

                Logger = serviceProvider.GetService<ILogger<Program>>();

                bot.JoinChannels(channels);
                
                using (host)
                {
                    await host.RunAsync();
                }
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

            _twitchApi = new TwitchAPI
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

            serviceCollection.AddSingleton<ITwitchAPI>(_twitchApi);

            var api = new OpenAIAPI();

            serviceCollection.AddSingleton<IOpenAIAPI>(api);

            var elevenLabsClient = new ElevenLabsClient(ElevenLabsAuthentication.LoadFromEnv());

            serviceCollection.AddSingleton(elevenLabsClient);
        }

        private static void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            _twitchApi.Auth.RefreshAuthTokenAsync(_twitchApi.Settings.AccessToken, _configuration["client_secret"], _configuration["client_id"]);
        }
    }
}
