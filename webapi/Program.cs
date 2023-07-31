using AspNet.Security.OAuth.Twitch;
using Infrastructure;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using Swashbuckle.AspNetCore.SwaggerUI;

DotEnv.Load();

var scopes = new Dictionary<string, string>()
{
    {"user:read:email", "Description"}
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "kinobotz API v1.0", Version = "v1" });

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows()
        {
            Implicit = new OpenApiOAuthFlow()
            {
                AuthorizationUrl = new Uri("https://id.twitch.tv/oauth2/authorize"),
                TokenUrl = new Uri("https://id.twitch.tv/oauth2/authorize?response_type=token"),
                Scopes = scopes
            }
        }
    });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme{
                    Reference = new OpenApiReference{
                        Id = "oauth2", //The name of the previously defined security scheme.
                        Type = ReferenceType.SecurityScheme
                    }
                },
                new List<string>()
            }
        });

});

builder.Services
    .AddAuthentication()
    .AddTwitch(options =>
    {
        options.ClientId = builder.Configuration["twitch_client_id"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["twitch_client_secret"] ?? string.Empty;

        foreach (var scope in scopes.Keys)
        {
            options.Scope.Add(scope);
        }

        options.SaveTokens = true;
    });

var redisConfig = new RedisConfiguration()
{
    ConnectionString = $"{builder.Configuration["redis_host"]},password={builder.Configuration["redis_password"]},allowAdmin=true"
};

builder.Services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig);

var app = builder.Build();

app.UseCors(a => a.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "kinobotz API v1.0");
    c.DocumentTitle = "kinobotz API";
    c.DocExpansion(DocExpansion.None);
    c.OAuthClientId(builder.Configuration["twitch_client_id"]);
    c.OAuthClientSecret(builder.Configuration["twitch_client_secret"]);
    c.OAuthAppName("kinobotz API");
    c.OAuthScopeSeparator(",");
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRedisInformation();
app.MapControllers();
app.Run();