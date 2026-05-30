using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Admin;

/// <summary>
/// All fields optional — only the provided ones are applied.
/// <c>Status</c>: <c>active</c> | <c>inactive</c>.
/// </summary>
public sealed record UpdateUserRequest(
    [MaxLength(255)] string? FullName,
    [MaxLength(20)] string? Phone,
    [RegularExpression("^(active|inactive)$")] string? Status,
    [RegularExpression("^(vi|en)$")] string? PreferredLanguage);
