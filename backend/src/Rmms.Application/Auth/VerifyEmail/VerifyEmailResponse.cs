namespace Rmms.Application.Auth.VerifyEmail;

public sealed record VerifyEmailResponse(
    Guid UserId,
    string Email,
    string Status);
