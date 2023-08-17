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
        private readonly ICommandRepository _commandRepository;

        public CommandsController(ICommandRepository commandRepository)
        {
            _commandRepository = commandRepository;
        }

        [HttpGet]
        public ActionResult<List<CommandInformation>> Get()
        {
            return Ok(Commands.DefaultCommands);
        }

        [HttpGet("counters")]
        [CustomClaimRequirement("AccessLevel", "Admin")]
        public async Task<ActionResult<IDictionary<string, long>>> GetCommandExecutionCounters()
        {
            return Ok(await _commandRepository.GetExecutionCounters());
        }
    }
}
