namespace Rmms.Application.Common.Abstractions;

/// <summary>A freshly issued approval email token: the signed string (goes in the
/// email URL), its SHA-256 hash (persisted for one-time-use tracking), and expiry.</summary>
public sealed record IssuedApprovalToken(string Token, string Hash, DateTimeOffset ExpiresAt);

/// <summary>Validated payload decoded from a signed approval token.</summary>
public sealed record ApprovalTokenPayload(
    Guid ApprovalId,
    Guid ApproverId,
    IReadOnlyList<string> ActionOptions,
    DateTimeOffset ExpiresAt,
    string Nonce);

/// <summary>
/// Issues + verifies HMAC-signed, JWT-like approval tokens for the BUH email-link
/// flow (M09, BR-407 / AC-18). Format: <c>base64url(header).base64url(payload).base64url(sig)</c>
/// where sig = HMAC-SHA256(header.payload, server secret).
/// </summary>
public interface IApprovalTokenService
{
    IssuedApprovalToken Issue(Guid approvalId, Guid approverId, IReadOnlyList<string> actionOptions, DateTimeOffset now);

    /// <summary>Returns the payload if the signature is valid and not expired; otherwise null.</summary>
    ApprovalTokenPayload? Verify(string token, DateTimeOffset now);
}
