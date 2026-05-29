using FluentValidation;

namespace Rmms.Application.Auth.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Token là bắt buộc.")
            .MinimumLength(10).WithErrorCode("INVALID_FORMAT").WithMessage("Token không hợp lệ.")
            .MaximumLength(128).WithErrorCode("INVALID_FORMAT").WithMessage("Token không hợp lệ.");
    }
}
