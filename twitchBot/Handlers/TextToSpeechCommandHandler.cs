using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElevenLabs;
using ElevenLabs.Voices;
using twitchBot.Commands;
using twitchBot.Entities;

namespace twitchBot.Handlers
{
    public class TextToSpeechCommandHandler : BaseCommandHandler<TextToSpeechCommand>
    {
        private readonly ElevenLabsClient elevenLabsClient;

        public TextToSpeechCommandHandler(ElevenLabsClient elevenLabsClient)
        {
            this.elevenLabsClient = elevenLabsClient;
        }

        public override async Task<Response> InternalHandle(TextToSpeechCommand request, CancellationToken cancellationToken)
        {
            var defaultVoiceSettings = await elevenLabsClient.VoicesEndpoint.GetDefaultVoiceSettingsAsync(cancellationToken);

            Voice voice;

            if (string.IsNullOrEmpty(request.Voice))
            {
                voice = (await elevenLabsClient.VoicesEndpoint.GetAllVoicesAsync(cancellationToken)).FirstOrDefault();
            }
            else
            {
                voice = await elevenLabsClient.VoicesEndpoint.GetVoiceAsync(request.Voice, cancellationToken: cancellationToken);
            }

            // TODO comentado para nao ficar criando arquivos
            //var clipPath = await elevenLabsClient.TextToSpeechEndpoint.TextToSpeechAsync(request.Message, voice, defaultVoiceSettings, cancellationToken: cancellationToken);

            return new Response
            {
                Message = "Audio gerado"
            };
        }
    }
}
