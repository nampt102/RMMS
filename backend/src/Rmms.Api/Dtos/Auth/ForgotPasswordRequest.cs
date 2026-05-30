using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Auth;

public sealed record ForgotPasswordRequest(
    [Required][EmailAddress][MaxLength(255)] string Email);
