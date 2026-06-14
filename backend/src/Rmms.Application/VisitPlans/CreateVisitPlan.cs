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

/// <summary>One planned store visit in a create/edit request (store + form to fill there).</summary>
public sealed record VisitPlanItemInput(Guid StoreId, Guid FormId);

/// <summary>
/// Leader creates a visit plan (M11, AC-28). Routes a BUH approval (BR-406) and links it.
/// The caller (controller) supplies the authenticated Leader id.
/// </summary>
public sealed record CreateVisitPlanCommand(
    Guid LeaderUserId, DateOnly VisitDate, string? Notes, IReadOnlyList<VisitPlanItemInput> Items)
    : IRequest<Result<VisitPlanDto>>;

public sealed class CreateVisitPlanCommandValidator : AbstractValidator<CreateVisitPlanCommand>
{
    public CreateVisitPlanCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateVisitPlanCommandHandler : IRequestHandler<CreateVisitPlanCommand, Result<VisitPlanDto>>
{
    private readonly IAppDbContext _db;
    private readonly IApprovalService _approvals;
    private readonly IAuditLogger _audit;

    public CreateVisitPlanCommandHandler(IAppDbContext db, IApprovalService approvals, IAuditLogger audit)
    {
        _db = db;
        _approvals = approvals;
        _audit = audit;
    }

    public async ValueTask<Result<VisitPlanDto>> Handle(CreateVisitPlanCommand command, CancellationToken ct)
    {
        var validation = await VisitPlanGuards.ValidateItemsAsync(_db, command.Items, ct);
        if (validation is not null) return Result.Failure<VisitPlanDto>(validation);

        VisitPlan plan;
        try
        {
            plan = VisitPlan.Create(
                command.LeaderUserId, command.VisitDate, command.Notes,
                command.Items.Select(i => new VisitItemInput(i.StoreId, i.FormId)).ToList());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<VisitPlanDto>(Error.Validation(ErrorCodes.ValidationFailed, ex.Message));
        }

        _db.VisitPlans.Add(plan);

        await VisitPlanProducer.RouteAsync(_db, _approvals, plan.Id, command.LeaderUserId, id => plan.LinkApproval(id), ct);

        await _audit.RecordAsync(AuditAction.VisitPlanCreated, "visit_plan", plan.Id,
            new { plan.LeaderUserId, plan.VisitDate, stores = command.Items.Count }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(VisitPlanMapper.ToDto(plan));
    }
}

/// <summary>Shared item validation: stores exist + active, forms exist + published.</summary>
internal static class VisitPlanGuards
{
    public static async Task<Error?> ValidateItemsAsync(
        IAppDbContext db, IReadOnlyList<VisitPlanItemInput> items, CancellationToken ct)
    {
        if (items is null || items.Count == 0)
        {
            return Error.Validation(ErrorCodes.ValidationFailed, "Cần ít nhất một cửa hàng trong kế hoạch.");
        }

        var storeIds = items.Select(i => i.StoreId).Distinct().ToList();
        var foundStores = await db.Stores.AsNoTracking()
            .Where(s => storeIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
        if (foundStores.Count != storeIds.Count)
        {
            return Error.Validation(ErrorCodes.ValidationFailed, "Một hoặc nhiều cửa hàng không tồn tại.");
        }

        var formIds = items.Select(i => i.FormId).Distinct().ToList();
        var publishedForms = await db.Forms.AsNoTracking()
            .Where(f => formIds.Contains(f.Id) && f.Status == FormStatus.Published && f.CurrentVersion > 0)
            .Select(f => f.Id).ToListAsync(ct);
        if (publishedForms.Count != formIds.Count)
        {
            return Error.Validation(ErrorCodes.ValidationFailed, "Một hoặc nhiều form chưa được phát hành.");
        }

        return null;
    }
}
