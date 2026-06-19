using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{
    /// <summary>Verifies the HMAC-SHA256 signature Twitch sends on every EventSub webhook
    /// request (Twitch-Eventsub-Message-Signature), computed over id + timestamp + raw body.</summary>
    public static class EventSubSignature
    {
        public static bool IsValid(string? secret, string? messageId, string? timestamp, string body, string? signatureHeader)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(messageId)
                || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signatureHeader))
            {
                return false;
            }

            var message = messageId + timestamp + body;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signatureHeader));
        }
    }
}
