using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.VisitPlan;
using Rmms.Shared.Errors;

namespace Rmms.Application.VisitPlans;

/// <summary>
/// Leader edits a still-pending visit plan in place (M11). Editing an approved plan is not allowed
/// in Phase 1 (re-approval is Phase 2 — see M11 edge cases). Only the owning Leader may edit.
/// </summary>
public sealed record EditVisitPlanCommand(
    Guid PlanId, Guid LeaderUserId, DateOnly VisitDate, string? Notes, IReadOnlyList<VisitPlanItemInput> Items)
    : IRequest<Result<VisitPlanDto>>;

public sealed class EditVisitPlanCommandValidator : AbstractValidator<EditVisitPlanCommand>
{
    public EditVisitPlanCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class EditVisitPlanCommandHandler : IRequestHandler<EditVisitPlanCommand, Result<VisitPlanDto>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public EditVisitPlanCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<VisitPlanDto>> Handle(EditVisitPlanCommand command, CancellationToken ct)
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
        if (!plan.IsPending)
        {
            return Result.Failure<VisitPlanDto>(Error.Conflict(ErrorCodes.Conflict, "Chỉ có thể sửa kế hoạch đang chờ duyệt."));
        }

        var validation = await VisitPlanGuards.ValidateItemsAsync(_db, command.Items, ct);
        if (validation is not null) return Result.Failure<VisitPlanDto>(validation);

        try
        {
            plan.ReplaceItems(command.Notes, command.Items.Select(i => new VisitItemInput(i.StoreId, i.FormId)).ToList());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<VisitPlanDto>(Error.Validation(ErrorCodes.ValidationFailed, ex.Message));
        }

        await _audit.RecordAsync(AuditAction.VisitPlanEdited, "visit_plan", plan.Id,
            new { plan.VisitDate, stores = command.Items.Count }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(VisitPlanMapper.ToDto(plan));
    }
}
