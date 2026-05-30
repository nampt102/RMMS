using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Admin;

public sealed record CreateUserRequest(
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MaxLength(255)] string FullName,
    [MaxLength(20)] string? Phone,
    [Required][RegularExpression("^(leader|buh|admin)$")] string Role,
    [Required][RegularExpression("^(vi|en)$")] string PreferredLanguage = "vi");
