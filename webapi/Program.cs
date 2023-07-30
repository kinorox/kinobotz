using Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

DotEnv.Load();

var scopes = new Dictionary<string, string>()
{
    {"user:read:email", "Description"}
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                },
                Scheme = "oauth2",
                Name = "oauth2",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
    })
    .AddTwitch(options =>
    {
        options.ClientId = builder.Configuration["twitch_client_id"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["twitch_client_secret"] ?? string.Empty;
        options.Scope.Add("user:read:email");
        options.SaveTokens = true;
        options.AuthorizationEndpoint = "https://localhost/";
        
    });

var redisConfig = new RedisConfiguration()
{
    ConnectionString = $"{builder.Configuration["redis_host"]},password={builder.Configuration["redis_password"]},allowAdmin=true"
};

builder.Services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.OAuthClientId(builder.Configuration["twitch_client_id"]);
        c.OAuthClientSecret(builder.Configuration["twitch_client_secret"]);
        c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
    });
}

app.UseHttpsRedirection();


app.UseAuthorization();
app.UseAuthentication();
app.UseRedisInformation();

app.MapControllers();

app.Run();