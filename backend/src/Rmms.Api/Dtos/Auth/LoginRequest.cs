using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

/// <summary>
/// Request body for <c>POST /api/v1/auth/login</c>.
/// <para>
/// <see cref="Device"/> is REQUIRED for PG (mobile, BR-105 device check) and OPTIONAL
/// for Leader / BUH / Admin (web). Web clients omit it; the device check is PG-scoped.
/// </para>
/// </summary>
public sealed record LoginRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MaxLength(128)] string Password,
    LoginDeviceDto? Device = null);

public sealed record LoginDeviceDto(
    [Required][MaxLength(255)] string DeviceId,
    [Required][MaxLength(255)] string DeviceName,
    [Required][RegularExpression("^(ios|android)$")] string Os,
    [MaxLength(20)] string OsVersion = "",
    [MaxLength(20)] string AppVersion = "",
    [MaxLength(500)] string? FcmToken = null);
