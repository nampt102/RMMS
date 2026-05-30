using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

public sealed record ResetPasswordRequest(
    [Required][MinLength(10)][MaxLength(128)] string Token,
    [Required][MinLength(8)][MaxLength(128)] string NewPassword);
