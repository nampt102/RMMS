using FluentValidation;

namespace Rmms.Application.Admin.Users.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.FullName).MaximumLength(255).When(x => x.FullName is not null);
        RuleFor(x => x.Phone).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Status)
            .Must(s => s is "active" or "inactive")
            .When(x => x.Status is not null)
            .WithErrorCode("INVALID_VALUE")
            .WithMessage("Status chỉ hỗ trợ 'active' hoặc 'inactive'.");

        RuleFor(x => x.PreferredLanguage)
            .Must(lang => lang is "vi" or "en")
            .When(x => x.PreferredLanguage is not null)
            .WithErrorCode("INVALID_VALUE");
    }
}
