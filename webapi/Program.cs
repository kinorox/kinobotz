using System.Text;
using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using webapi.Formatters;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["jwt"]))
        };
    });


var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddLogging(config => config.AddSerilog(logger, true));
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
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IBotConnectionRepository, BotConnectionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new ByteArrayInputFormatter());
});
builder.Services.AddTransient<IGptRepository, GptRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "kinobotz API", Version = "v1" });

    // Configure JWT authentication for Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "kinobotz API v1");
});

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<OverlayHub>("/overlayHub");
app.UseHttpsRedirection();
app.MapControllers();
app.UseRedisInformation();
app.Run();