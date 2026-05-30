using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Admin.Users.GetUsers;

internal sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedResponse<AdminUserDto>>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;

    public GetUsersQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<AdminUserDto>>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.Users.AsNoTracking().AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(query.Role) && TryParseRole(query.Role, out var role))
        {
            q = q.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && TryParseStatus(query.Status, out var status))
        {
            q = q.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            q = q.Where(u => EF.Functions.ILike(u.Email, $"%{s}%") || EF.Functions.ILike(u.FullName, $"%{s}%"));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Phone,
                u.Role.ToSnakeCase(),
                u.Status.ToSnakeCase(),
                u.PreferredLanguage,
                u.EmailVerifiedAt,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync(ct);

        return new PaginatedResponse<AdminUserDto>(items, page, pageSize, total);
    }

    private static bool TryParseRole(string raw, out UserRole role)
    {
        switch (raw.ToLowerInvariant())
        {
            case "pg": role = UserRole.Pg; return true;
            case "leader": role = UserRole.Leader; return true;
            case "buh": role = UserRole.Buh; return true;
            case "admin": role = UserRole.Admin; return true;
            default: role = default; return false;
        }
    }

    private static bool TryParseStatus(string raw, out UserStatus status)
    {
        switch (raw.ToLowerInvariant())
        {
            case "active": status = UserStatus.Active; return true;
            case "inactive": status = UserStatus.Inactive; return true;
            case "pending_email_verify":
            case "pending": status = UserStatus.PendingEmailVerify; return true;
            default: status = default; return false;
        }
    }
}
