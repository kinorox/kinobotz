using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace twitchBot.Handlers
{
    public sealed record ClipCreatorOptions(TimeSpan PollInterval, TimeSpan WaitPerAttempt, int MaxAttempts)
    {
        // Twitch: "if Get Clips doesn't return the clip after 15 seconds, assume it failed".
        public static readonly ClipCreatorOptions Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15), 2);
    }

    /// <summary>
    /// Creates a Twitch clip and waits for it to finish processing. Twitch accepts the create
    /// request immediately but builds the clip asynchronously, and sometimes never finishes it,
    /// so a clip that doesn't show up in time is requested again.
    /// </summary>
    public sealed class ClipCreator
    {
        private readonly TimeProvider _timeProvider;
        private readonly ILogger _logger;
        private readonly ClipCreatorOptions _options;

        public ClipCreator(TimeProvider timeProvider, ILogger logger, ClipCreatorOptions options = null)
        {
            _timeProvider = timeProvider;
            _logger = logger;
            _options = options ?? ClipCreatorOptions.Default;
        }

        /// <param name="createClip">Requests a clip; returns its id, or null if Twitch returned none.</param>
        /// <param name="getClipUrl">Looks a clip up by id; returns its url, or null while it is still processing.</param>
        /// <returns>The clip url, or null if no attempt produced a clip.</returns>
        public async Task<string> Create(string channel, Func<Task<string>> createClip, Func<string, Task<string>> getClipUrl, CancellationToken cancellationToken = default)
        {
            for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
            {
                string clipId;
                try
                {
                    clipId = await createClip();
                }
                catch (Exception e)
                {
                    // e.g. the channel is offline or has clips disabled: retrying won't help.
                    _logger.LogWarning(e, "{Channel} - create clip request failed", channel);
                    return null;
                }

                if (string.IsNullOrEmpty(clipId))
                {
                    _logger.LogWarning("{Channel} - create clip returned no clip id (attempt {Attempt})", channel, attempt);
                    continue;
                }

                _logger.LogInformation("{Channel} - clip {ClipId} requested (attempt {Attempt})", channel, clipId, attempt);

                var url = await WaitForClip(channel, clipId, getClipUrl, cancellationToken);
                if (url != null)
                {
                    return url;
                }

                _logger.LogWarning("{Channel} - clip {ClipId} not available after {Wait}s (attempt {Attempt})",
                    channel, clipId, _options.WaitPerAttempt.TotalSeconds, attempt);
            }

            return null;
        }

        private async Task<string> WaitForClip(string channel, string clipId, Func<string, Task<string>> getClipUrl, CancellationToken cancellationToken)
        {
            var deadline = _timeProvider.GetUtcNow() + _options.WaitPerAttempt;

            while (true)
            {
                await Task.Delay(_options.PollInterval, _timeProvider, cancellationToken);

                try
                {
                    var url = await getClipUrl(clipId);
                    if (!string.IsNullOrEmpty(url))
                    {
                        return url;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "{Channel} - error checking clip {ClipId}", channel, clipId);
                }

                if (_timeProvider.GetUtcNow() >= deadline)
                {
                    return null;
                }
            }
        }
    }
}
