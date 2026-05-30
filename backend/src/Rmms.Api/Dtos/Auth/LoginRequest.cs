using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

/// <summary>
/// Request body for <c>POST /api/v1/auth/login</c>.
/// Device info MUST be sent on every login (BR-105 device check).
/// </summary>
public sealed record LoginRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MaxLength(128)] string Password,
    [Required] LoginDeviceDto Device);

public sealed record LoginDeviceDto(
    [Required][MaxLength(255)] string DeviceId,
    [Required][MaxLength(255)] string DeviceName,
    [Required][RegularExpression("^(ios|android)$")] string Os,
    [MaxLength(20)] string OsVersion = "",
    [MaxLength(20)] string AppVersion = "",
    [MaxLength(500)] string? FcmToken = null);
