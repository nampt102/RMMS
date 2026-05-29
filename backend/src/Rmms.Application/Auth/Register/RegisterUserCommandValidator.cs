using FluentValidation;

namespace Rmms.Application.Auth.Register;

/// <summary>
/// Validation for <see cref="RegisterUserCommand"/>:
///   - Email: valid format, ≤255 chars.
///   - Password: ≥8 chars, at least 1 letter + 1 digit (per M01 spec; complexity tightening Phase 2).
///   - FullName: ≥1 char, ≤255 chars.
///   - Phone: optional, ≤20 chars if provided.
///   - PreferredLanguage: must be "vi" or "en".
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Email là bắt buộc.")
            .EmailAddress().WithErrorCode("INVALID_FORMAT").WithMessage("Email không hợp lệ.")
            .MaximumLength(255).WithErrorCode("TOO_LONG").WithMessage("Email tối đa 255 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Mật khẩu là bắt buộc.")
            .MinimumLength(8).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Must(HasLetter).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 1 chữ cái.")
            .Must(HasDigit).WithErrorCode("PASSWORD_TOO_WEAK").WithMessage("Mật khẩu phải có ít nhất 1 chữ số.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Họ tên là bắt buộc.")
            .MaximumLength(255).WithErrorCode("TOO_LONG").WithMessage("Họ tên tối đa 255 ký tự.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithErrorCode("TOO_LONG").WithMessage("Số điện thoại tối đa 20 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Ngôn ngữ là bắt buộc.")
            .Must(lang => lang is "vi" or "en")
            .WithErrorCode("INVALID_VALUE")
            .WithMessage("Ngôn ngữ chỉ hỗ trợ 'vi' hoặc 'en'.");
    }

    private static bool HasLetter(string? value) =>
        !string.IsNullOrEmpty(value) && value.Any(char.IsLetter);

    private static bool HasDigit(string? value) =>
        !string.IsNullOrEmpty(value) && value.Any(char.IsDigit);
}
