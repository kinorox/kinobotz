using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using ElevenLabs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI_API;
using Serilog;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using twitchBot.Commands;
using twitchBot.Hubs;
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
            try
            {
                DotEnv.Load(); 
                
                //var aTimer = new Timer();
                //aTimer.Elapsed += OnTimedAccessToken;
                //aTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
                //aTimer.Enabled = true;

                await Host.CreateDefaultBuilder(args)
                    .ConfigureServices(services =>
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

                        services.AddLogging(config => config.AddSerilog(logger, true));
                        services.AddSingleton(_configuration);
                        var redisConfig = new RedisConfiguration()
                        {
                            ConnectionString =
                                $"{_configuration["redis_host"]},password={_configuration["redis_password"]},allowAdmin=true"
                        };
                        services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig);
                        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
                        services.AddTransient<IBot, Bot>();

                        _twitchApi = new TwitchAPI
                        {
                            Settings =
                            {
                                ClientId = _configuration["client_id"],
                                Secret = _configuration["client_secret"],
                                AccessToken = _configuration["access_token"]
                            }
                        };

                        services.AddSingleton<ITwitchAPI>(_twitchApi);

                        var api = new OpenAIAPI();

                        services.AddSingleton<IOpenAIAPI>(api);

                        var elevenLabsClient = new ElevenLabsClient(ElevenLabsAuthentication.LoadFromEnv());

                        services.AddSingleton(elevenLabsClient);
                        services.AddSingleton<ICommandFactory, CommandFactory>();
                        services.AddHostedService<Bot>();
                        services.AddSignalR();
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseUrls("http://*:5000");
                        webBuilder.CaptureStartupErrors(true);
                        webBuilder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
                    })
                    .Build()
                    .RunAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            var response = await _twitchApi.Auth.RefreshAuthTokenAsync(_configuration["refresh_token"],
                _configuration["client_secret"], _configuration["client_id"]);
            _twitchApi.Settings.AccessToken = response.AccessToken;
        }
    }

    class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<AudioHub>("/audioHub");
            });
        }
    }
}
