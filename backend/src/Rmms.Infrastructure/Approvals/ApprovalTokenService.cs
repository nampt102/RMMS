using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Options;
using Rmms.Application.Common.Security;

namespace Rmms.Infrastructure.Approvals;

/// <summary>
/// HMAC-SHA256 signed, JWT-like approval token (M09, BR-407). Format
/// <c>base64url(header).base64url(payload).base64url(sig)</c>. The full token string
/// goes in the BUH email URL; only its SHA-256 hash is persisted (one-time use).
/// </summary>
internal sealed class ApprovalTokenService : IApprovalTokenService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly byte[] _key;
    private readonly int _ttlHours;

    public ApprovalTokenService(IOptions<ApprovalOptions> options)
    {
        var opt = options.Value;
        // A blank key is only viable for local/dev/test; use a deterministic dev key so
        // tokens still round-trip. Production sets Approval:SigningKey via env/user-secrets.
        var key = string.IsNullOrWhiteSpace(opt.SigningKey)
            ? "rmms-dev-approval-signing-key-change-me"
            : opt.SigningKey;
        _key = Encoding.UTF8.GetBytes(key);
        _ttlHours = opt.TokenTtlHours <= 0 ? 24 : opt.TokenTtlHours;
    }

    private sealed record Payload(string Aid, string Uid, string[] Act, long Exp, string N);

    public IssuedApprovalToken Issue(Guid approvalId, Guid approverId, IReadOnlyList<string> actionOptions, DateTimeOffset now)
    {
        var expires = now.AddHours(_ttlHours);
        var header = B64(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"RMMSAPV"}"""));
        var payloadObj = new Payload(
            Aid: approvalId.ToString("N"),
            Uid: approverId.ToString("N"),
            Act: actionOptions.ToArray(),
            Exp: expires.ToUnixTimeSeconds(),
            N: B64(RandomNumberGenerator.GetBytes(16)));
        var payload = B64(JsonSerializer.SerializeToUtf8Bytes(payloadObj, Json));
        var signingInput = $"{header}.{payload}";
        var sig = B64(Sign(signingInput));
        var token = $"{signingInput}.{sig}";
        return new IssuedApprovalToken(token, OpaqueToken.Hash(token), expires);
    }

    public ApprovalTokenPayload? Verify(string token, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            var expectedSig = Sign($"{parts[0]}.{parts[1]}");
            var actualSig = FromB64(parts[2]);
            if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig)) return null;

            var payload = JsonSerializer.Deserialize<Payload>(FromB64(parts[1]), Json);
            if (payload is null) return null;

            var expires = DateTimeOffset.FromUnixTimeSeconds(payload.Exp);
            if (now >= expires) return null;
            if (!Guid.TryParseExact(payload.Aid, "N", out var aid)) return null;
            if (!Guid.TryParseExact(payload.Uid, "N", out var uid)) return null;

            return new ApprovalTokenPayload(aid, uid, payload.Act ?? Array.Empty<string>(), expires, payload.N);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return null;
        }
    }

    private byte[] Sign(string input)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
    }

    private static string B64(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromB64(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };
        return Convert.FromBase64String(padded);
    }
}
