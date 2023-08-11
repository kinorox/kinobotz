using Infrastructure.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace webapi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class GptController : ControllerBase
    {
        private readonly IGptRepository _gptRepository;

        public GptController(IGptRepository gptRepository)
        {
            _gptRepository = gptRepository;
        }

        [HttpGet("~/behaviors")]
        public async Task<ActionResult<IDictionary<string, string?>>> Get()
        {
            var claimsPrincipal = HttpContext.User;

            var id = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _gptRepository.GetGptBehaviors(id);

            return Ok(result);
        }
    }
}
