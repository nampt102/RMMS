using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Application.Common;

/// <summary>
/// Visibility gate for the hidden super-admin (M01). A super-admin account is invisible to every
/// other user — only another super-admin may list, view, or manage it. Handlers use this to filter
/// lists and to 404 mutations that target a super-admin from a non-super caller.
/// </summary>
public static class SuperAdminAccess
{
    public static async Task<bool> CallerIsSuperAdminAsync(IAppDbContext db, Guid? callerId, CancellationToken ct)
        => callerId is { } id && await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsSuperAdmin, ct);
}
