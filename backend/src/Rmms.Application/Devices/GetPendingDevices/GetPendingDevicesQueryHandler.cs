using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Devices.GetPendingDevices;

internal sealed class GetPendingDevicesQueryHandler
    : IRequestHandler<GetPendingDevicesQuery, Result<IReadOnlyList<PendingDeviceDto>>>
{
    private readonly IAppDbContext _db;

    public GetPendingDevicesQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<PendingDeviceDto>>> Handle(GetPendingDevicesQuery query, CancellationToken ct)
    {
        var pending = _db.UserDevices
            .AsNoTracking()
            .Where(d => d.Status == DeviceStatus.PendingApproval);

        // Leaders only see requests from PGs they actively manage (M03 BR scoping).
        if (!query.IsAdmin)
        {
            var managedPgIds = _db.UserLeaderAssignments
                .AsNoTracking()
                .Where(a => a.LeaderUserId == query.CallerUserId && a.EffectiveTo == null)
                .Select(a => a.PgUserId);
            pending = pending.Where(d => managedPgIds.Contains(d.UserId));
        }

        var rows = await pending
            .Join(_db.Users, d => d.UserId, u => u.Id, (d, u) => new
            {
                d.Id,
                d.UserId,
                u.Email,
                u.FullName,
                u.Role,
                d.DeviceName,
                d.Os,
                d.OsVersion,
                d.AppVersion,
                d.CreatedAt,
            })
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        IReadOnlyList<PendingDeviceDto> result = rows
            .Select(x => new PendingDeviceDto(
                x.Id,
                x.UserId,
                x.Email,
                x.FullName,
                x.Role.ToSnakeCase(),
                x.DeviceName,
                x.Os,
                x.OsVersion,
                x.AppVersion,
                x.CreatedAt))
            .ToList();

        return Result.Success(result);
    }
}
