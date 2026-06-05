using FluentValidation;

namespace Rmms.Application.Devices.RejectDevice;

public sealed class RejectDeviceCommandValidator : AbstractValidator<RejectDeviceCommand>
{
    public RejectDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithErrorCode("REJECT_REASON_REQUIRED").WithMessage("Lý do từ chối là bắt buộc.")
            .MaximumLength(500);
    }
}
