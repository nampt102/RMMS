using FluentValidation;

namespace Rmms.Application.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode("REQUIRED")
            .MinimumLength(10).MaximumLength(128);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Mật khẩu mới là bắt buộc.")
            .MinimumLength(8).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Must(HasLetter).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 1 chữ cái.")
            .Must(HasDigit).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 1 chữ số.");
    }

    private static bool HasLetter(string? v) => !string.IsNullOrEmpty(v) && v.Any(char.IsLetter);
    private static bool HasDigit(string? v) => !string.IsNullOrEmpty(v) && v.Any(char.IsDigit);
}
