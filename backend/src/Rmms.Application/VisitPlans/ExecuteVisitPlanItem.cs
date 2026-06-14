using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.VisitPlans;

/// <summary>
/// Leader links a post-visit Form Engine submission to one item of an approved plan (M11, AC-30).
/// The submission must be the Leader's own and target the item's planned form. When every item is
/// linked the plan flips to <c>executed</c>.
/// </summary>
public sealed record ExecuteVisitItemCommand(Guid PlanId, Guid ItemId, Guid LeaderUserId, Guid FormSubmissionId)
    : IRequest<Result<VisitPlanDto>>;

internal sealed class ExecuteVisitItemCommandHandler : IRequestHandler<ExecuteVisitItemCommand, Result<VisitPlanDto>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public ExecuteVisitItemCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<VisitPlanDto>> Handle(ExecuteVisitItemCommand command, CancellationToken ct)
    {
        var plan = await _db.VisitPlans.FirstOrDefaultAsync(p => p.Id == command.PlanId, ct);
        if (plan is null)
        {
            return Result.Failure<VisitPlanDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy kế hoạch viếng thăm."));
        }
        if (plan.LeaderUserId != command.LeaderUserId)
        {
            return Result.Failure<VisitPlanDto>(Error.Forbidden(ErrorCodes.PermissionDenied, "Bạn không phải chủ kế hoạch này."));
        }
        if (plan.Status is not (VisitPlanStatus.Approved or VisitPlanStatus.Executed))
        {
            return Result.Failure<VisitPlanDto>(Error.Conflict(ErrorCodes.Conflict, "Chỉ có thể thực hiện kế hoạch đã được duyệt."));
        }

        var item = plan.Items.FirstOrDefault(i => i.Id == command.ItemId);
        if (item is null)
        {
            return Result.Failure<VisitPlanDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm viếng thăm."));
        }

        var submission = await _db.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == command.FormSubmissionId, ct);
        if (submission is null)
        {
            return Result.Failure<VisitPlanDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy bài nộp form."));
        }
        if (submission.UserId != command.LeaderUserId)
        {
            return Result.Failure<VisitPlanDto>(Error.Forbidden(ErrorCodes.PermissionDenied, "Bài nộp không thuộc về bạn."));
        }
        if (submission.FormId != item.FormId)
        {
            return Result.Failure<VisitPlanDto>(Error.Validation(ErrorCodes.ValidationFailed, "Bài nộp không khớp form của điểm viếng thăm."));
        }

        try
        {
            plan.ExecuteItem(command.ItemId, command.FormSubmissionId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<VisitPlanDto>(Error.Conflict(ErrorCodes.Conflict, ex.Message));
        }

        await _audit.RecordAsync(AuditAction.VisitPlanItemExecuted, "visit_plan", plan.Id,
            new { item = command.ItemId, submission = command.FormSubmissionId, status = plan.Status.ToString() }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(VisitPlanMapper.ToDto(plan));
    }
}
