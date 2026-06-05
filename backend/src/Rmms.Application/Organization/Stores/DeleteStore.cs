using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Stores;

/// <summary>Soft-delete a store (M03). The soft-delete interceptor (ADR-004) sets deleted_at.</summary>
public sealed record DeleteStoreCommand(Guid StoreId) : IRequest<Result>;

internal sealed class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteStoreCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteStoreCommand command, CancellationToken ct)
    {
        var store = await _db.Stores.SingleOrDefaultAsync(s => s.Id == command.StoreId, ct);
        if (store is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm bán."));
        }

        _db.Stores.Remove(store); // → soft delete via interceptor

        await _audit.RecordAsync(
            action: AuditAction.StoreDeleted,
            targetEntity: "store",
            targetId: store.Id,
            metadata: new { store.Code },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
