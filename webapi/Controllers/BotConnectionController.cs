using Entities;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
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

        return Ok(botConnections.Values);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] BotConnection botConnection)
    {
        var existing = await redisClient.Db0.GetAsync<BotConnection>($"botconnection:{id}");

        if (existing == null) return NotFound();

        // update existing property values with the new ones
        existing.RefreshToken = botConnection.RefreshToken;
        existing.ChannelId = botConnection.ChannelId;
        existing.Login = botConnection.Login;
        existing.AccessToken = botConnection.AccessToken;
        existing.Active = botConnection.Active;
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

        await redisClient.Db0.PublishAsync(new RedisChannel("NewBotConnection", RedisChannel.PatternMode.Literal), botConnection.Id.ToString());

        return Ok();
    }
}
