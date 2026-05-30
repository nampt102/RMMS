namespace Rmms.Application.Admin.Users;

/// <summary>Shared DTO for Admin user list + detail.</summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    string Status,
    string PreferredLanguage,
    DateTimeOffset? EmailVerifiedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
