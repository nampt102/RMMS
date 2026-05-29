using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

/// <summary>
/// Request body for <c>POST /api/v1/auth/register</c>.
///
/// Validation: DataAnnotations here drive Swagger schema + ASP.NET model binding;
/// the authoritative server-side rules live in <c>RegisterUserCommandValidator</c> (FluentValidation).
///
/// Note (.NET 10): for positional records, attributes go on the parameter directly
/// (no <c>[property: ...]</c> prefix) so the new validation source-generator picks them up.
/// </summary>
public sealed record RegisterRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MinLength(8)][MaxLength(128)] string Password,
    [Required][MaxLength(255)] string FullName,
    [MaxLength(20)] string? Phone,
    [Required][RegularExpression("^(vi|en)$")] string PreferredLanguage = "vi");
