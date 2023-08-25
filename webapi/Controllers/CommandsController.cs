using Entities;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Attributes;

namespace webapi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class CommandsController : ControllerBase
    {
        private readonly IBotConnectionRepository _botConnectionRepository;

        public CommandsController(IBotConnectionRepository botConnectionRepository)
        {
            _botConnectionRepository = botConnectionRepository;
        }

        [HttpGet]
        public ActionResult<List<Command>> Get()
        {
            return Ok(Commands.DefaultCommands);
        }

        [HttpGet("counters")]
        [CustomClaimRequirement("AccessLevel", "Admin")]
        public async Task<ActionResult<IDictionary<string, long>>> GetCommandExecutionCounters()
        {
            var response = await _botConnectionRepository.GetExecutionCounters();
            return Ok(response.OrderBy(r => r.Key));
        }
    }
}
