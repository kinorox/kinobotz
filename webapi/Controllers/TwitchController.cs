using Microsoft.AspNetCore.Mvc;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Microsoft.AspNetCore.Authentication;

namespace webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TwitchController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IBotConnectionRepository _botConnectionRepository;
        private readonly ILogger<TwitchController> _logger;

        public TwitchController(IConfiguration configuration, IJwtService jwtService, IBotConnectionRepository botConnectionRepository, ILogger<TwitchController> logger)
        {
            _configuration = configuration;
            _jwtService = jwtService;
            _botConnectionRepository = botConnectionRepository;
            _logger = logger;
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Sign out the user's authentication session
            HttpContext.SignOutAsync();

            return Ok(new { message = "Signed out successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> TwitchLogin([FromBody] TwitchAccessTokenModel request)
        {
            var tokenValidationResult = await VerifyTwitchToken(request);

            if (!tokenValidationResult.IsValid) return Unauthorized();

            var user = await AssociateOrCreateUser(tokenValidationResult.User, tokenValidationResult);
            var sessionToken = GenerateSessionToken(user);
            
            return Ok(new
            {
                AccessToken = sessionToken
            });
        }

        private async Task<TwitchTokenValidationResult> VerifyTwitchToken(TwitchAccessTokenModel request)
        {
            var twitchClientId = _configuration["twitch_client_id"];
            var twitchClientSecret = _configuration["twitch_client_secret"];

            try
            {
                var twitchApi = new TwitchAPI();

                var accessTokenResponse = await twitchApi.Auth.GetAccessTokenFromCodeAsync(clientId: twitchClientId, code: request.AccessToken, clientSecret: twitchClientSecret, redirectUri: request.RedirectUri);

                twitchApi.Settings.AccessToken = accessTokenResponse.AccessToken;
                twitchApi.Settings.ClientId= twitchClientId;
                twitchApi.Settings.Secret = twitchClientSecret;

                var userResponse = await twitchApi.Helix.Users.GetUsersAsync(accessToken: accessTokenResponse.AccessToken);
                
                return new TwitchTokenValidationResult
                {
                    IsValid = true,
                    User = userResponse.Users[0],
                    AccessToken = accessTokenResponse.AccessToken,
                    RefreshToken = accessTokenResponse.RefreshToken
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during twitch authentication.");
                return new TwitchTokenValidationResult { IsValid = false };
            }
        }

        private async Task<BotConnection> AssociateOrCreateUser(User user, TwitchTokenValidationResult twitchTokenValidationResult)
        {
            var existingUser = await _botConnectionRepository.GetByChannelId(user.Id);

            if (existingUser != null)
            {
                existingUser.AccessToken = twitchTokenValidationResult.AccessToken;
                existingUser.RefreshToken = twitchTokenValidationResult.RefreshToken;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _botConnectionRepository.SaveOrUpdate(existingUser);

                return existingUser;
            }

            var newBotConnection = new BotConnection()
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Active = true,
                ChannelId = user.Id,
                Login = user.Login,
                AccessToken = twitchTokenValidationResult.AccessToken,
                RefreshToken = twitchTokenValidationResult.RefreshToken,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl
            };

            await _botConnectionRepository.SaveOrUpdate(newBotConnection);
            
            return newBotConnection;
        }

        private string GenerateSessionToken(BotConnection user)
        {
            var token = _jwtService.GenerateToken(user);

            return token;
        }
    }
    
    public class TwitchTokenValidationResult
    {
        public bool IsValid { get; set; }
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
        public User User { get; set; }
    }

    public class TwitchAccessTokenModel
    {
        public string? RedirectUri { get; set; }
        public string? AccessToken { get; set; }
    }
}
