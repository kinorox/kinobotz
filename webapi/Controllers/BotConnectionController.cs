using Entities;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace webapi.Controllers;

[ApiController]
[Route("[controller]")]
public class BotConnectionController : ControllerBase
{
    private readonly IRedisClient redisClient;
    
    public BotConnectionController(IRedisClient redisClient)
    {
        this.redisClient = redisClient;
    }

    [HttpGet]
    public async Task<ActionResult<IDictionary<string, BotConnection>>> Get()
    {
        var existingKeys = await redisClient.Db0.SearchKeysAsync("botconnection*");

        var botConnections = await redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

        foreach (var connection in botConnections)
        {
            if (connection.Value == null) continue;

            connection.Value.RefreshToken = connection.Value.RefreshToken.Mask(0, connection.Value.RefreshToken.Length - 5, '*');
            connection.Value.AccessToken = connection.Value.AccessToken.Mask(0, connection.Value.AccessToken.Length - 5, '*');
            connection.Value.DiscordTtsWebhookUrl = connection.Value.DiscordTtsWebhookUrl.Mask(0, connection.Value.DiscordTtsWebhookUrl.Length - 5, '*');
            connection.Value.DiscordClipsWebhookUrl = connection.Value.DiscordClipsWebhookUrl.Mask(0, connection.Value.DiscordClipsWebhookUrl.Length - 5, '*');
        }

        return Ok(botConnections.Values);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] BotConnection botConnection)
    {
        var existing = await redisClient.Db0.GetAsync<BotConnection>($"botconnection:{id}");

        if (existing == null) return NotFound();

        // update existing property values with the new ones

        //existing.RefreshToken = botConnection.RefreshToken;
        existing.ChannelId = botConnection.ChannelId;
        existing.Login = botConnection.Login;
        //existing.AccessToken = botConnection.AccessToken;
        existing.Active = botConnection.Active;
        //existing.DiscordClipsWebhookUrl = botConnection.DiscordClipsWebhookUrl;
        //existing.DiscordTtsWebhookUrl = botConnection.DiscordTtsWebhookUrl;
        existing.UpdatedAt = DateTime.UtcNow;

        await redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", botConnection);

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] BotConnection botConnection)
    {
        botConnection.Id = Guid.NewGuid();
        botConnection.CreatedAt = DateTime.UtcNow;
        botConnection.Active = true;

        await redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", botConnection);

        //await redisClient.Db0.PublishAsync(new RedisChannel("NewBotConnection", RedisChannel.PatternMode.Literal), botConnection.Id.ToString());

        return Ok();
    }
}