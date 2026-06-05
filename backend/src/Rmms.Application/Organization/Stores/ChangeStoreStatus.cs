using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Stores;

/// <summary>
/// Activate / deactivate a store (M03). Deactivating does NOT cascade to ongoing
/// schedules — per M03 edge case they keep working until their end date.
/// </summary>
public sealed record ChangeStoreStatusCommand(Guid StoreId, string Status) : IRequest<Result>;

public sealed class ChangeStoreStatusCommandValidator : AbstractValidator<ChangeStoreStatusCommand>
{
    public ChangeStoreStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithErrorCode("REQUIRED")
            .Must(s => s is "active" or "inactive").WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class ChangeStoreStatusCommandHandler : IRequestHandler<ChangeStoreStatusCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public ChangeStoreStatusCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(ChangeStoreStatusCommand command, CancellationToken ct)
    {
        var store = await _db.Stores.SingleOrDefaultAsync(s => s.Id == command.StoreId, ct);
        if (store is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm bán."));
        }

        if (command.Status == "active") store.Activate();
        else store.Deactivate();

        await _audit.RecordAsync(
            action: AuditAction.StoreStatusChanged,
            targetEntity: "store",
            targetId: store.Id,
            metadata: new { status = command.Status },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
