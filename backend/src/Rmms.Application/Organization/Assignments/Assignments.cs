using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Assignments;

// ===== DTOs =====

public sealed record AssignedRefDto(Guid Id, string Code, string Name);

public sealed record UserAssignmentsDto(
    Guid UserId,
    AssignedLeaderDto? Leader,
    IReadOnlyList<AssignedRefDto> Stores,
    IReadOnlyList<AssignedRefDto> Categories);

public sealed record AssignedLeaderDto(Guid LeaderUserId, string FullName, string Email);

// ===== PG → Leader =====

/// <summary>
/// Assign a PG to a Leader (M03, 1:1 active). Re-assigning ends the previous active
/// assignment (edge case: pending requests stay with the old Leader — not touched here).
/// </summary>
public sealed record AssignPgLeaderCommand(Guid PgUserId, Guid LeaderUserId) : IRequest<Result>;

internal sealed class AssignPgLeaderCommandHandler : IRequestHandler<AssignPgLeaderCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public AssignPgLeaderCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(AssignPgLeaderCommand command, CancellationToken ct)
    {
        var pg = await _db.Users.SingleOrDefaultAsync(u => u.Id == command.PgUserId, ct);
        if (pg is null || pg.Role != UserRole.Pg)
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidAssignment, "Người được phân công phải là PG hợp lệ."));
        }

        var leader = await _db.Users.SingleOrDefaultAsync(u => u.Id == command.LeaderUserId, ct);
        if (leader is null || leader.Role != UserRole.Leader)
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidAssignment, "Người quản lý phải là Leader hợp lệ."));
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        var current = await _db.UserLeaderAssignments
            .SingleOrDefaultAsync(a => a.PgUserId == command.PgUserId && a.EffectiveTo == null, ct);

        if (current is not null)
        {
            if (current.LeaderUserId == command.LeaderUserId)
            {
                return Result.Success(); // already assigned to this leader — idempotent no-op
            }
            // End the old assignment effective today (kept for history).
            current.End(today);
        }

        var assignment = UserLeaderAssignment.Create(command.PgUserId, command.LeaderUserId, today);
        _db.UserLeaderAssignments.Add(assignment);

        await _audit.RecordAsync(
            AuditAction.PgLeaderAssigned, "user_leader_assignment", assignment.Id,
            new { pg_user_id = command.PgUserId, leader_user_id = command.LeaderUserId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== User ↔ Store =====

public sealed record AssignUserStoreCommand(Guid UserId, Guid StoreId) : IRequest<Result>;

internal sealed class AssignUserStoreCommandHandler : IRequestHandler<AssignUserStoreCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public AssignUserStoreCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(AssignUserStoreCommand command, CancellationToken ct)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == command.UserId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Người dùng không tồn tại."));
        }
        if (!await _db.Stores.AnyAsync(s => s.Id == command.StoreId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Điểm bán không tồn tại."));
        }

        var exists = await _db.UserStoreAssignments
            .AnyAsync(a => a.UserId == command.UserId && a.StoreId == command.StoreId && a.EffectiveTo == null, ct);
        if (exists)
        {
            return Result.Failure(Error.Conflict(ErrorCodes.AssignmentExists, "Người dùng đã được gán điểm bán này."));
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var assignment = UserStoreAssignment.Create(command.UserId, command.StoreId, today);
        _db.UserStoreAssignments.Add(assignment);

        await _audit.RecordAsync(
            AuditAction.UserStoreAssigned, "user_store_assignment", assignment.Id,
            new { user_id = command.UserId, store_id = command.StoreId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record UnassignUserStoreCommand(Guid UserId, Guid StoreId) : IRequest<Result>;

internal sealed class UnassignUserStoreCommandHandler : IRequestHandler<UnassignUserStoreCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public UnassignUserStoreCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(UnassignUserStoreCommand command, CancellationToken ct)
    {
        var assignment = await _db.UserStoreAssignments
            .SingleOrDefaultAsync(a => a.UserId == command.UserId && a.StoreId == command.StoreId && a.EffectiveTo == null, ct);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy phân công điểm bán đang hiệu lực."));
        }

        assignment.End(DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime));

        await _audit.RecordAsync(
            AuditAction.UserStoreUnassigned, "user_store_assignment", assignment.Id,
            new { user_id = command.UserId, store_id = command.StoreId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== User ↔ Category =====

public sealed record AssignUserCategoryCommand(Guid UserId, Guid CategoryId) : IRequest<Result>;

internal sealed class AssignUserCategoryCommandHandler : IRequestHandler<AssignUserCategoryCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public AssignUserCategoryCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(AssignUserCategoryCommand command, CancellationToken ct)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == command.UserId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Người dùng không tồn tại."));
        }
        if (!await _db.Categories.AnyAsync(c => c.Id == command.CategoryId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Ngành hàng không tồn tại."));
        }

        var exists = await _db.UserCategoryAssignments
            .AnyAsync(a => a.UserId == command.UserId && a.CategoryId == command.CategoryId, ct);
        if (exists)
        {
            return Result.Failure(Error.Conflict(ErrorCodes.AssignmentExists, "Người dùng đã được gán ngành hàng này."));
        }

        var assignment = UserCategoryAssignment.Create(command.UserId, command.CategoryId);
        _db.UserCategoryAssignments.Add(assignment);

        await _audit.RecordAsync(
            AuditAction.UserCategoryAssigned, "user_category_assignment", assignment.Id,
            new { user_id = command.UserId, category_id = command.CategoryId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record UnassignUserCategoryCommand(Guid UserId, Guid CategoryId) : IRequest<Result>;

internal sealed class UnassignUserCategoryCommandHandler : IRequestHandler<UnassignUserCategoryCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UnassignUserCategoryCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UnassignUserCategoryCommand command, CancellationToken ct)
    {
        var assignment = await _db.UserCategoryAssignments
            .SingleOrDefaultAsync(a => a.UserId == command.UserId && a.CategoryId == command.CategoryId, ct);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy phân công ngành hàng."));
        }

        _db.UserCategoryAssignments.Remove(assignment);

        await _audit.RecordAsync(
            AuditAction.UserCategoryUnassigned, "user_category_assignment", assignment.Id,
            new { user_id = command.UserId, category_id = command.CategoryId }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Query: all assignments for one user (web detail panel) =====

public sealed record GetUserAssignmentsQuery(Guid UserId) : IRequest<Result<UserAssignmentsDto>>;

internal sealed class GetUserAssignmentsQueryHandler : IRequestHandler<GetUserAssignmentsQuery, Result<UserAssignmentsDto>>
{
    private readonly IAppDbContext _db;

    public GetUserAssignmentsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<UserAssignmentsDto>> Handle(GetUserAssignmentsQuery query, CancellationToken ct)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == query.UserId, ct))
        {
            return Result.Failure<UserAssignmentsDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng."));
        }

        var leaderRow = await _db.UserLeaderAssignments.AsNoTracking()
            .Where(a => a.PgUserId == query.UserId && a.EffectiveTo == null)
            .Join(_db.Users.AsNoTracking(), a => a.LeaderUserId, u => u.Id, (a, u) => new { u.Id, u.FullName, u.Email })
            .FirstOrDefaultAsync(ct);
        var leader = leaderRow is null ? null : new AssignedLeaderDto(leaderRow.Id, leaderRow.FullName, leaderRow.Email);

        var storeRows = await _db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == query.UserId && a.EffectiveTo == null)
            .Join(_db.Stores.AsNoTracking(), a => a.StoreId, s => s.Id, (a, s) => new { s.Id, s.Code, s.Name })
            .OrderBy(s => s.Code)
            .ToListAsync(ct);
        IReadOnlyList<AssignedRefDto> stores = storeRows.Select(s => new AssignedRefDto(s.Id, s.Code, s.Name)).ToList();

        var categoryRows = await _db.UserCategoryAssignments.AsNoTracking()
            .Where(a => a.UserId == query.UserId)
            .Join(_db.Categories.AsNoTracking(), a => a.CategoryId, c => c.Id, (a, c) => new { c.Id, c.Code, c.Name })
            .OrderBy(c => c.Code)
            .ToListAsync(ct);
        IReadOnlyList<AssignedRefDto> categories = categoryRows.Select(c => new AssignedRefDto(c.Id, c.Code, c.Name)).ToList();

        return Result.Success(new UserAssignmentsDto(query.UserId, leader, stores, categories));
    }
}
