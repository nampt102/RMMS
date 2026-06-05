using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Areas;

// ----- DTO -----

public sealed record AreaDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentAreaId,
    string? ParentAreaName,
    DateTimeOffset CreatedAt);

// ===== Create =====

public sealed record CreateAreaCommand(string Code, string Name, Guid? ParentAreaId) : IRequest<Result<Guid>>;

public sealed class CreateAreaCommandValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
    }
}

internal sealed class CreateAreaCommandHandler : IRequestHandler<CreateAreaCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateAreaCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateAreaCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();
        if (await _db.Areas.AnyAsync(a => a.Code == code, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(ErrorCodes.CodeAlreadyExists, "Mã khu vực đã tồn tại."));
        }

        if (command.ParentAreaId is { } parentId && !await _db.Areas.AnyAsync(a => a.Id == parentId, ct))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.InvalidReference, "Khu vực cha không tồn tại."));
        }

        var area = Area.Create(code, command.Name, command.ParentAreaId);
        _db.Areas.Add(area);

        await _audit.RecordAsync(AuditAction.AreaCreated, "area", area.Id, new { area.Code, area.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(area.Id);
    }
}

// ===== Update =====

public sealed record UpdateAreaCommand(Guid AreaId, string Name, Guid? ParentAreaId) : IRequest<Result>;

public sealed class UpdateAreaCommandValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
}

internal sealed class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateAreaCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateAreaCommand command, CancellationToken ct)
    {
        var area = await _db.Areas.SingleOrDefaultAsync(a => a.Id == command.AreaId, ct);
        if (area is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy khu vực."));
        }

        if (command.ParentAreaId is { } parentId)
        {
            if (parentId == command.AreaId)
            {
                return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Khu vực không thể là cha của chính nó."));
            }
            if (!await _db.Areas.AnyAsync(a => a.Id == parentId, ct))
            {
                return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Khu vực cha không tồn tại."));
            }
        }

        area.Update(command.Name, command.ParentAreaId);

        await _audit.RecordAsync(AuditAction.AreaUpdated, "area", area.Id, new { area.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Delete (soft) =====

public sealed record DeleteAreaCommand(Guid AreaId) : IRequest<Result>;

internal sealed class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteAreaCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteAreaCommand command, CancellationToken ct)
    {
        var area = await _db.Areas.SingleOrDefaultAsync(a => a.Id == command.AreaId, ct);
        if (area is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy khu vực."));
        }

        if (await _db.Stores.AnyAsync(s => s.AreaId == command.AreaId, ct))
        {
            return Result.Failure(Error.Conflict(ErrorCodes.Conflict, "Không thể xoá: vẫn còn điểm bán thuộc khu vực này."));
        }

        _db.Areas.Remove(area);
        await _audit.RecordAsync(AuditAction.AreaDeleted, "area", area.Id, new { area.Code }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== List =====

public sealed record GetAreasQuery : IRequest<Result<IReadOnlyList<AreaDto>>>;

internal sealed class GetAreasQueryHandler : IRequestHandler<GetAreasQuery, Result<IReadOnlyList<AreaDto>>>
{
    private readonly IAppDbContext _db;

    public GetAreasQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<AreaDto>>> Handle(GetAreasQuery query, CancellationToken ct)
    {
        var rows = await _db.Areas.AsNoTracking()
            .OrderBy(a => a.Code)
            .GroupJoin(_db.Areas.AsNoTracking(), a => a.ParentAreaId, p => (Guid?)p.Id, (a, parents) => new { a, parents })
            .SelectMany(x => x.parents.DefaultIfEmpty(), (x, p) => new { x.a, ParentName = p != null ? p.Name : null })
            .ToListAsync(ct);

        IReadOnlyList<AreaDto> result = rows
            .Select(x => new AreaDto(x.a.Id, x.a.Code, x.a.Name, x.a.ParentAreaId, x.ParentName, x.a.CreatedAt))
            .ToList();

        return Result.Success(result);
    }
}
