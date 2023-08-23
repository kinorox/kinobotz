using Microsoft.AspNetCore.Mvc;
using Infrastructure.Repository;

namespace webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublicController : ControllerBase
    {
        private readonly IGptRepository _gptRepository;
        private readonly IBotConnectionRepository _botConnectionRepository;

        public PublicController(IGptRepository gptRepository, IBotConnectionRepository botConnectionRepository)
        {
            _gptRepository = gptRepository;
            _botConnectionRepository = botConnectionRepository;
        }

        [HttpGet("gptBehaviors")]
        [HttpGet("gptBehaviors/{channel}")]
        public async Task<ActionResult<BehaviorDefinition[]>> Get(string? channel)
        {
            BehaviorDefinition[] result;

            if (string.IsNullOrEmpty(channel))
            {
                result = await _gptRepository.GetAllGptBehaviors();
            }
            else
            {
                var botConnection = _botConnectionRepository.GetByLogin(channel);

                result = await _gptRepository.GetGptBehaviors(botConnection.Id.ToString());
            }

            return Ok(result.OrderByDescending(r => r.CreatedAt));
        }
    }
}
