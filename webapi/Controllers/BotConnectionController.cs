using Entities;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace webapi.Controllers;

[ApiController]
[Route("[controller]")]
public class BotConnectionController : ControllerBase
{

    private readonly ILogger<BotConnectionController> logger;
    private readonly IRedisClient redisClient;
    
    public BotConnectionController(ILogger<BotConnectionController> logger, IRedisClient redisClient)
    {
        this.logger = logger;
        this.redisClient = redisClient;
    }

    [HttpGet(Name = "GetBotConnection")]
    public async Task<IDictionary<string, BotConnection>> Get()
    {
        var existingKeys = await redisClient.Db0.SearchKeysAsync("botconnection*");

        var botConnections = await redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

        return botConnections;
    }

    [HttpPut(Name = "UpdateBotConnection")]
    public async Task<IResult> Put([FromBody] BotConnection botConnection)
    {
        var existing = await redisClient.Db0.GetAsync<BotConnection>($"botconnection:{botConnection.Id}");
        
        await redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", botConnection);

        return Results.Ok();
    }

    [HttpPost(Name = "CreateBotConnection")]
    public async Task<IResult> Post([FromBody] BotConnection botConnection)
    {
        botConnection.Id = Guid.NewGuid();

        await redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", botConnection);

        await redisClient.Db0.PublishAsync(new RedisChannel("NewBotConnection", RedisChannel.PatternMode.Literal), botConnection.Id.ToString());

        return Results.Ok();
    }
}
