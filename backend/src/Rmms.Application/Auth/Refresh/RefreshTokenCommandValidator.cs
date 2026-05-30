using FluentValidation;

namespace Rmms.Application.Auth.Refresh;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Refresh token là bắt buộc.")
            .MinimumLength(10).WithErrorCode("INVALID_FORMAT").WithMessage("Refresh token không hợp lệ.")
            .MaximumLength(256);
    }
}
