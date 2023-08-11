using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
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
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Twitch";
    })
    .AddCookie()
    .AddOAuth("Twitch", options =>
    {
        options.ClientId = builder.Configuration["twitch_client_id"];
        options.ClientSecret = builder.Configuration["twitch_client_secret"];
        options.CallbackPath = "/"; // The callback URL after authentication on Twitch.
        options.AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
        options.TokenEndpoint = "https://id.twitch.tv/oauth2/token";
        options.UserInformationEndpoint = "https://api.twitch.tv/helix/users";
        options.Scope.Add("user:read:email");
        options.Scope.Add("analytics:read:games");
        options.Scope.Add("user:edit:broadcast");
        options.Scope.Add("channel:read:subscriptions");
        options.Scope.Add("channel:read:redemptions");
        options.Scope.Add("channel:manage:broadcast");
        options.Scope.Add("user:read:subscriptions");
        options.Scope.Add("user:read:follows");
        options.Scope.Add("channel:read:polls");
        options.Scope.Add("channel:read:predictions");
        options.Scope.Add("channel:read:vips");
        options.Scope.Add("channel:read:vips");
        options.ClaimActions.MapJsonKey("urn:twitch:id", "id");
        options.ClaimActions.MapJsonKey("urn:twitch:login", "login");
        options.ClaimActions.MapJsonKey("urn:twitch:name", "display_name");
        options.SaveTokens = true;
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = context =>
            {
                return Task.CompletedTask;
                // Handle the creation of the user account and saving data in your database.
                // context.Identity contains user information.
            }
        };
    });

builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
    corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((_) => true)
            .AllowCredentials();
    }));
builder.Services.AddSingleton<IOverlayHub, OverlayHub>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IBotConnectionRepository, BotConnectionRepository>();
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
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<OverlayHub>("/overlayHub");
app.UseHttpsRedirection();
app.MapControllers();
app.UseRedisInformation();
app.Run();