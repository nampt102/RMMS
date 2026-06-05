using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Stores;

/// <summary>Update store profile + GPS coords (M03). Code is immutable post-create.</summary>
public sealed record UpdateStoreCommand(
    Guid StoreId,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    Guid? AreaId) : IRequest<Result>;

public sealed class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateStoreCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateStoreCommand command, CancellationToken ct)
    {
        var store = await _db.Stores.SingleOrDefaultAsync(s => s.Id == command.StoreId, ct);
        if (store is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy điểm bán."));
        }

        if (command.AreaId is { } areaId && !await _db.Areas.AnyAsync(a => a.Id == areaId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Khu vực không tồn tại."));
        }

        store.Update(command.Name, command.Address, command.Latitude, command.Longitude, command.AreaId);

        await _audit.RecordAsync(
            action: AuditAction.StoreUpdated,
            targetEntity: "store",
            targetId: store.Id,
            metadata: new { store.Name, store.AreaId },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
