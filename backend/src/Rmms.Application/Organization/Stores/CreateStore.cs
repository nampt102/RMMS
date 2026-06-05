using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Stores;

/// <summary>Create a retail store with GPS coords (M03). Admin-only at the controller.</summary>
public sealed record CreateStoreCommand(
    string Code,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    Guid? AreaId) : IRequest<Result<Guid>>;

public sealed class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithErrorCode("INVALID_VALUE");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithErrorCode("INVALID_VALUE");
    }
}

internal sealed class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateStoreCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateStoreCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();

        if (await _db.Stores.AnyAsync(s => s.Code == code, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(ErrorCodes.CodeAlreadyExists, "Mã điểm bán đã tồn tại."));
        }

        if (command.AreaId is { } areaId && !await _db.Areas.AnyAsync(a => a.Id == areaId, ct))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.InvalidReference, "Khu vực không tồn tại."));
        }

        var store = Store.Create(code, command.Name, command.Address, command.Latitude, command.Longitude, command.AreaId);
        _db.Stores.Add(store);

        await _audit.RecordAsync(
            action: AuditAction.StoreCreated,
            targetEntity: "store",
            targetId: store.Id,
            metadata: new { store.Code, store.Name, store.AreaId },
            ct: ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success(store.Id);
    }
}
