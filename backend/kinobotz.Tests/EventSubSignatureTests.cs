using System.Security.Cryptography;
using System.Text;
using Infrastructure.Services;

namespace kinobotz.Tests;

// Verifies the EventSub webhook HMAC signature check (id + timestamp + body).
public class EventSubSignatureTests
{
    private const string Secret = "s3cr3t-value";
    private const string Id = "msg-1";
    private const string Ts = "2026-06-18T00:00:00Z";
    private const string Body = "{\"subscription\":{\"type\":\"channel.cheer\"}}";

    private static string Sign(string secret, string id, string ts, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(id + ts + body));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void Valid_signature_passes()
    {
        var sig = Sign(Secret, Id, Ts, Body);
        Assert.True(EventSubSignature.IsValid(Secret, Id, Ts, Body, sig));
    }

    [Fact]
    public void Tampered_body_fails()
    {
        var sig = Sign(Secret, Id, Ts, Body);
        Assert.False(EventSubSignature.IsValid(Secret, Id, Ts, "{\"tampered\":true}", sig));
    }

    [Fact]
    public void Wrong_secret_fails()
    {
        var sig = Sign(Secret, Id, Ts, Body);
        Assert.False(EventSubSignature.IsValid("different-secret", Id, Ts, Body, sig));
    }

    [Fact]
    public void Missing_signature_fails()
    {
        Assert.False(EventSubSignature.IsValid(Secret, Id, Ts, Body, null));
    }
}
