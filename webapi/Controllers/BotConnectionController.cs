using Entities;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AutoMapper;
using webapi.Dto;

namespace webapi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class BotConnectionController : ControllerBase
{
    private readonly IBotConnectionRepository _botConnectionRepository;
    private readonly IMapper _mapper;
    
    public BotConnectionController(IBotConnectionRepository botConnectionRepository, IMapper mapper)
    {
        _botConnectionRepository = botConnectionRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<BotConnectionDto>>> Get()
    {
        var botConnections = await _botConnectionRepository.GetAll();

        if (!botConnections.Any())
            return NotFound();

        var mapped = _mapper.Map<ICollection<BotConnectionDto>>(botConnections);

        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BotConnectionDto>> Get(string id)
    {
        var botConnection = await _botConnectionRepository.GetById(id);

        if (botConnection == null)
            return NotFound();

        var mapped = _mapper.Map<BotConnectionDto>(botConnection);
        
        return Ok(mapped);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<BotConnectionDto>> GetProfile()
    {
        var claimsPrincipal = HttpContext.User;
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var botConnection = await _botConnectionRepository.GetById(userId);
        
        if (botConnection == null)
            return NotFound();

        var mapped = _mapper.Map<BotConnectionDto>(botConnection);
        
        return Ok(mapped);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateBotConnection botConnection)
    {
        var claimsPrincipal = HttpContext.User;

        var id = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var existing = await _botConnectionRepository.GetById(id);

        if (existing == null)
            return NotFound();

        existing.Active = botConnection.Active;
        existing.DiscordClipsWebhookUrl = botConnection.DiscordClipsWebhookUrl;
        existing.DiscordTtsWebhookUrl = botConnection.DiscordTtsWebhookUrl;
        existing.Commands = botConnection.Commands;
        existing.UpdatedAt = DateTime.UtcNow;

        await _botConnectionRepository.SaveOrUpdate(existing);

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

        await _botConnectionRepository.SaveOrUpdate(botConnection);

        return Ok();
    }
}

public class UpdateBotConnection
{
    public bool? Active { get; set; }
    public string? DiscordClipsWebhookUrl { get; set; }
    public string? DiscordTtsWebhookUrl { get; set; }
    public Dictionary<string, bool> Commands { get; set; }
}
