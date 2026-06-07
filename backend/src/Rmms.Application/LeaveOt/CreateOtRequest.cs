using FluentValidation;
using Mediator;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;

namespace Rmms.Application.LeaveOt;

public sealed record CreateOtRequestCommand(
    Guid UserId, DateOnly OtDate, TimeOnly StartTime, TimeOnly EndTime, string Reason)
    : IRequest<Result<OtRequestDto>>;

public sealed class CreateOtRequestCommandValidator : AbstractValidator<CreateOtRequestCommand>
{
    public CreateOtRequestCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(1000);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class CreateOtRequestCommandHandler : IRequestHandler<CreateOtRequestCommand, Result<OtRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalService _approvals;
    private readonly IAuditLogger _audit;

    public CreateOtRequestCommandHandler(IAppDbContext db, IApprovalService approvals, IAuditLogger audit)
    {
        _db = db;
        _approvals = approvals;
        _audit = audit;
    }

    public async ValueTask<Result<OtRequestDto>> Handle(CreateOtRequestCommand command, CancellationToken ct)
    {
        var request = OtRequest.Create(command.UserId, command.OtDate, command.StartTime, command.EndTime, command.Reason);
        _db.OtRequests.Add(request);

        await LeaveOtProducer.RouteAsync(_db, _approvals, ApprovalEntityType.OtRequest, request.Id, command.UserId,
            id => request.LinkApproval(id), ct);

        await _audit.RecordAsync(AuditAction.OtRequested, "ot_request", request.Id,
            new { request.UserId, request.OtDate, request.StartTime, request.EndTime }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(LeaveOtMapper.ToDto(request));
    }
}
