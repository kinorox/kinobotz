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
        private readonly IAuditLogRepository _auditLogRepository;

        public CommandsController(IBotConnectionRepository botConnectionRepository, IAuditLogRepository auditLogRepository)
        {
            _botConnectionRepository = botConnectionRepository;
            _auditLogRepository = auditLogRepository;
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

        [HttpGet("log")]
        [CustomClaimRequirement("AccessLevel", "Admin")]
        public async Task<ActionResult<AuditLog[]>> GetAuditLog()
        {
            var response = await _auditLogRepository.Get();

            return Ok(response.OrderByDescending(r => r.Timestamp));
        }
    }
}
