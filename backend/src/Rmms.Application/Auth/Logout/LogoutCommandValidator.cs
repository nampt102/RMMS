using FluentValidation;

namespace Rmms.Application.Auth.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Refresh token là bắt buộc.")
            .MaximumLength(256);
    }
}
