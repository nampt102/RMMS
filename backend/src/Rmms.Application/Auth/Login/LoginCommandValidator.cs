using FluentValidation;

namespace Rmms.Application.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Email là bắt buộc.")
            .EmailAddress().WithErrorCode("INVALID_FORMAT").WithMessage("Email không hợp lệ.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Mật khẩu là bắt buộc.")
            .MaximumLength(128);

        // Device is optional at the transport level: required for PG is enforced in the
        // handler (where the role is known). When present, it must be well-formed.
        When(x => x.Device is not null, () =>
        {
            RuleFor(x => x.Device!.DeviceId)
                .NotEmpty().WithErrorCode("REQUIRED").WithMessage("DeviceId là bắt buộc.")
                .MaximumLength(255);

            RuleFor(x => x.Device!.DeviceName)
                .NotEmpty().WithErrorCode("REQUIRED").WithMessage("Tên thiết bị là bắt buộc.")
                .MaximumLength(255);

            RuleFor(x => x.Device!.Os)
                .NotEmpty().WithErrorCode("REQUIRED")
                .Must(os => os is "ios" or "android")
                .WithErrorCode("INVALID_VALUE")
                .WithMessage("OS chỉ hỗ trợ 'ios' hoặc 'android'.");

            RuleFor(x => x.Device!.OsVersion).MaximumLength(20);
            RuleFor(x => x.Device!.AppVersion).MaximumLength(20);
            RuleFor(x => x.Device!.FcmToken).MaximumLength(500);
        });
    }
}
