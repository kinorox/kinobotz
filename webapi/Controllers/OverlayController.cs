using Infrastructure.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers
{
    [ApiController]
    public class OverlayController : ControllerBase
    {
        private readonly IOverlayHub overlayHub;

        public OverlayController(IOverlayHub overlayHub)
        {
            this.overlayHub = overlayHub;
        }

        [HttpPost("~/overlay/audio/{botConnectionId}")]
        public async Task<IActionResult> Post(string botConnectionId, [FromBody] byte[] audioStream)
        {
            await overlayHub.SendAudioStream(botConnectionId, audioStream);

            return Ok();
        }
    }
}
