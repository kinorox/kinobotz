using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Repository;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using Swashbuckle.AspNetCore.SwaggerUI;
using webapi.Formatters;

DotEnv.Load();

var scopes = new Dictionary<string, string>()
{
    {"user:read:email", "Description"}
};

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOverlayHub, OverlayHub>();
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new ByteArrayInputFormatter());
});
builder.Services.AddTransient<IGptRepository, GptRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "kinobotz API v1.0", Version = "v1" });
});

var redisConfig = new RedisConfiguration()
{
    ConnectionString = $"{builder.Configuration["redis_host"]},password={builder.Configuration["redis_password"]},allowAdmin=true"
};

builder.Services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig);
builder.WebHost.UseUrls($"http://*:{Environment.GetEnvironmentVariable("PORT") ?? "5000"}");
builder.WebHost.CaptureStartupErrors(true);
builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "kinobotz API v1.0");
    c.DocumentTitle = "kinobotz API";
    c.DocExpansion(DocExpansion.None);
});

app.MapHub<OverlayHub>("/overlayHub");
app.UseHttpsRedirection();
app.MapControllers();
app.UseRedisInformation();
app.Run();