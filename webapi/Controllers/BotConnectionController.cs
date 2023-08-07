using Entities;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace webapi.Controllers;

[ApiController]
//[Authorize]
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

            if(!string.IsNullOrEmpty(connection.Value.RefreshToken))
                connection.Value.RefreshToken = connection.Value.RefreshToken.Mask(0, connection.Value.RefreshToken.Length - 5, '*');

            if (!string.IsNullOrEmpty(connection.Value.AccessToken))
                connection.Value.AccessToken = connection.Value.AccessToken.Mask(0, connection.Value.AccessToken.Length - 5, '*');

            if (!string.IsNullOrEmpty(connection.Value.DiscordTtsWebhookUrl))
                connection.Value.DiscordTtsWebhookUrl = connection.Value.DiscordTtsWebhookUrl.Mask(0, connection.Value.DiscordTtsWebhookUrl.Length - 5, '*');
            
            if (!string.IsNullOrEmpty(connection.Value.DiscordClipsWebhookUrl))
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

        if(!string.IsNullOrEmpty(botConnection.RefreshToken))
            existing.RefreshToken = botConnection.RefreshToken;

        if (!string.IsNullOrEmpty(botConnection.ChannelId))
            existing.ChannelId = botConnection.ChannelId;

        if (!string.IsNullOrEmpty(botConnection.Login))
            existing.Login = botConnection.Login;

        if (!string.IsNullOrEmpty(botConnection.AccessToken))
            existing.AccessToken = botConnection.AccessToken;

        if (botConnection.Active != null)
            existing.Active = botConnection.Active;

        if (!string.IsNullOrEmpty(botConnection.DiscordClipsWebhookUrl))
            existing.DiscordClipsWebhookUrl = botConnection.DiscordClipsWebhookUrl;

        if (!string.IsNullOrEmpty(botConnection.DiscordTtsWebhookUrl))
            existing.DiscordTtsWebhookUrl = botConnection.DiscordTtsWebhookUrl;

        existing.UpdatedAt = DateTime.UtcNow;

        await redisClient.Db0.AddAsync($"botconnection:{id}", existing);

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

    [HttpPost("{login}")]
    public async Task<IActionResult> CreateFromLogin(string login)
    {
        var botConnection = new BotConnection()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Active = true,
            Login = login
        };

        await redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", botConnection);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await redisClient.Db0.RemoveAsync($"botconnection:{id}");

        return Ok();
    }
}
