using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

public sealed record RefreshTokenRequest(
    [Required][MinLength(10)][MaxLength(256)] string RefreshToken);
