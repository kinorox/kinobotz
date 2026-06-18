using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class KinobotzService : IKinobotzService
    {

        private readonly HttpClient _httpClient;
        private readonly IConfiguration configuration;
        private readonly ILogger<KinobotzService> logger;

        public KinobotzService(IConfiguration configuration, ILogger<KinobotzService> logger, HttpClient httpClient)
        {
            this.configuration = configuration;
            this.logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["KINOBOTZ_API_URL"]);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }

        public async Task SendAudioStream(string botConnectionId, byte[] audioStream)
        {
            try
            {
                using HttpClient client = new HttpClient { BaseAddress = new Uri(configuration["KINOBOTZ_API_URL"]) };
                
                var content = new ByteArrayContent(audioStream);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                using var httpResponseMessage = await _httpClient.PostAsync($"overlay/audio/{botConnectionId}", content);

                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error sending audio stream to kinobotz-api");
                throw;
            }
        }
    }

    public interface IKinobotzService
    {
        Task SendAudioStream(string botConnectionId, byte[] audioStream);
    }
}
