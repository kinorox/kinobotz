using System;
using System.IO;
using System.Threading.Tasks;
using ElevenLabs;
using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Repository;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI_API;
using Quartz;
using Serilog;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using twitchBot.Commands;
using twitchBot.Jobs;

namespace twitchBot
{
    class Program
    {
        private static IConfiguration _configuration;

        static async Task Main(string[] args)
        {
            try
            {
                DotEnv.Load();

                await Host.CreateDefaultBuilder(args)
                    .ConfigureServices(services =>
                    {
                        // Build configuration
                        var fullName = Directory.GetParent(AppContext.BaseDirectory)?.FullName;
                        if (fullName != null)
                            _configuration = new ConfigurationBuilder()
                                .SetBasePath(fullName)
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
                        services.AddMediatR(typeof(Program));
                        services.AddTransient<IBot, Bot>();
                        
                        var api = new OpenAIAPI();

                        services.AddSingleton<IOpenAIAPI>(api);

                        var elevenLabsClient = new ElevenLabsClient(ElevenLabsAuthentication.LoadFromEnv());

                        services.AddSingleton(elevenLabsClient);
                        services.AddTransient<ICommandFactory, CommandFactory>();
                        services.AddSingleton<Orchestrator>();
                        services.AddHostedService(p => p.GetRequiredService<Orchestrator>());
                        services.AddSignalR();
                        services.AddSingleton<IOverlayHub, OverlayHub>();
                        services.AddCors(options => options.AddPolicy("CorsPolicy",
                            builder =>
                            {
                                builder.AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .SetIsOriginAllowed((_) => true)
                                    .AllowCredentials();
                            }));
                        services.AddTransient<ICommandRepository, CommandRepository>();
                        services.AddTransient<IGptRepository, GptRepository>();
                        services.AddQuartz(q =>
                        {
                            q.UseMicrosoftDependencyInjectionJobFactory();
                            var jobKey = new JobKey("RefreshConnectionsJob");
                            
                            q.AddJob<RefreshConnectionsJob>(opts => opts.WithIdentity(jobKey));
                            
                            q.AddTrigger(opts => opts
                                .ForJob(jobKey) 
                                .WithIdentity("RefreshConnectionsJob-trigger")
                                .WithCronSchedule("0/30 * * * * ?"));
                        });
                        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseUrls($"http://*:{Environment.GetEnvironmentVariable("PORT") ?? "5000"}");
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
            app.UseCors("CorsPolicy");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OverlayHub>("/overlayHub");
            });
        }
    }
}
