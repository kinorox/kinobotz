using Entities;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AutoMapper;
using webapi.Dto;
using webapi.Attributes;

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
    [CustomClaimRequirement("AccessLevel", "Admin")]
    public async Task<ActionResult<ICollection<BotConnectionDto>>> Get()
    {
        var botConnections = await _botConnectionRepository.GetAll(true);

        if (!botConnections.Any())
            return NotFound();

        var mapped = _mapper.Map<ICollection<BotConnectionDto>>(botConnections);

        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BotConnectionDto>> Get(string id)
    {
        var botConnection = await _botConnectionRepository.GetById(id, true);

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

        var botConnection = await _botConnectionRepository.GetById(userId, true);
        
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
        existing.UpdatedAt = DateTime.UtcNow;
        existing.ChannelCommands = botConnection.ChannelCommands;

        await _botConnectionRepository.SaveOrUpdate(existing);

        if (botConnection.ChannelCommands == null) return Ok();

        //channel commands list should always have the same length as the default commands list
        if (botConnection.ChannelCommands.Count != Commands.DefaultCommands.Count)
            return BadRequest();

        //the prefixes should always be the same as the ones in the defaultcommands list
        if (botConnection.ChannelCommands.Any(command => Commands.DefaultCommands.All(x => x.Prefix != command.Prefix)))
        {
            return BadRequest();
        }

        //same for the descriptions
        if (botConnection.ChannelCommands.Any(command =>
                Commands.DefaultCommands.All(x => x.Description != command.Description)))
        {
            return BadRequest();
        }

        await _botConnectionRepository.SetCommands(existing.Id, botConnection.ChannelCommands);

        return Ok();
    }
}

public class UpdateBotConnection
{
    public bool? Active { get; set; }
    public string? DiscordClipsWebhookUrl { get; set; }
    public string? DiscordTtsWebhookUrl { get; set; }
    public ICollection<Command>? ChannelCommands { get; set; }
}
