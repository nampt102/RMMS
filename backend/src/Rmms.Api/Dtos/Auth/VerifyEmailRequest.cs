using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

/// <summary>
/// Request body for <c>POST /api/v1/auth/verify-email</c>.
/// Token is the plaintext from the verification email URL.
///
/// Note (.NET 10): attributes on positional record parameters (no <c>[property: ...]</c> prefix)
/// so the new validation source-generator picks them up.
/// </summary>
public sealed record VerifyEmailRequest(
    [Required][MinLength(10)][MaxLength(128)] string Token);
