namespace Rmms.Application.Auth.Me;

/// <summary>Profile + current-device projection returned by <c>GET /auth/me</c>.</summary>
public sealed record MeDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    string Status,
    string PreferredLanguage,
    DateTimeOffset? EmailVerifiedAt,
    DateTimeOffset? LastLoginAt,
    MeDeviceDto? CurrentDevice);

/// <summary>The active device tied to the current access token (null if not resolvable).</summary>
public sealed record MeDeviceDto(
    Guid Id,
    string DeviceId,
    string DeviceName,
    string Os,
    string OsVersion,
    string AppVersion,
    string Status,
    DateTimeOffset? LastUsedAt);
