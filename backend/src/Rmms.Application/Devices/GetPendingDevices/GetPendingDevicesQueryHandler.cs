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
        var rows = await _db.UserDevices
            .AsNoTracking()
            .Where(d => d.Status == DeviceStatus.PendingApproval)
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
