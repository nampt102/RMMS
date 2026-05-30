using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;

namespace Rmms.Application.Auth.Me;

internal sealed class GetDeviceStatusQueryHandler : IRequestHandler<GetDeviceStatusQuery, Result<DeviceStatusDto>>
{
    private readonly IAppDbContext _db;

    public GetDeviceStatusQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<DeviceStatusDto>> Handle(GetDeviceStatusQuery query, CancellationToken ct)
    {
        // Web users (Leader/BUH/Admin) carry an empty device_id claim — not device-bound.
        if (query.DeviceId is not { } deviceId || deviceId == Guid.Empty)
        {
            return new DeviceStatusDto("none", null, null, null);
        }

        var device = await _db.UserDevices
            .AsNoTracking()
            .Where(d => d.Id == deviceId && d.UserId == query.UserId)
            .Select(d => new { d.Id, d.DeviceName, d.Status, d.CreatedAt })
            .FirstOrDefaultAsync(ct);

        if (device is null)
        {
            return new DeviceStatusDto("unknown", deviceId, null, null);
        }

        return new DeviceStatusDto(device.Status.ToSnakeCase(), device.Id, device.DeviceName, device.CreatedAt);
    }
}
