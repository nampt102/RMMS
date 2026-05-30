using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

public sealed record LogoutRequest(
    [Required][MaxLength(256)] string RefreshToken);
