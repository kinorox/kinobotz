using Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GptController : ControllerBase
    {
        private readonly IGptRepository gptRepository;

        public GptController(IGptRepository gptRepository)
        {
            this.gptRepository = gptRepository;
        }

        [HttpGet("~/behaviors")]
        [HttpGet("~/behaviors/{botConnectionId}")]
        public async Task<ActionResult<IDictionary<string, string?>>> Get(string? botConnectionId)
        {
            var result = await gptRepository.GetAllGptBehaviors(botConnectionId);

            return Ok(result);
        }
    }
}
