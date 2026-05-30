using FluentValidation;

namespace Rmms.Application.Admin.Users.CreateAdminUser;

public sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("REQUIRED")
            .EmailAddress().WithErrorCode("INVALID_FORMAT")
            .MaximumLength(255);

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode("REQUIRED")
            .MaximumLength(255);

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Role)
            .NotEmpty().WithErrorCode("REQUIRED")
            .Must(r => r is "leader" or "buh" or "admin")
            .WithErrorCode("INVALID_VALUE")
            .WithMessage("Vai trò chỉ hỗ trợ 'leader', 'buh', 'admin'. PG phải tự đăng ký.");

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .Must(lang => lang is "vi" or "en")
            .WithErrorCode("INVALID_VALUE");
    }
}
