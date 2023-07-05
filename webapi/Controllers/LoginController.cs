using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpGet("~/login")]
        public IActionResult Login()
        {
            return Challenge(TwitchAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
