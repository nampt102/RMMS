using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;

namespace Rmms.Application.Devices.GetMyDevice;

internal sealed class GetMyDeviceQueryHandler : IRequestHandler<GetMyDeviceQuery, Result<MyDeviceDto>>
{
    private readonly IAppDbContext _db;

    public GetMyDeviceQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<MyDeviceDto>> Handle(GetMyDeviceQuery query, CancellationToken ct)
    {
        var devices = await _db.UserDevices
            .AsNoTracking()
            .Where(d => d.UserId == query.UserId
                && (d.Status == DeviceStatus.Active || d.Status == DeviceStatus.PendingApproval))
            .ToListAsync(ct);

        var active = Map(devices.FirstOrDefault(d => d.Status == DeviceStatus.Active));
        var pending = Map(devices.FirstOrDefault(d => d.Status == DeviceStatus.PendingApproval));

        return Result.Success(new MyDeviceDto(active, pending));
    }

    private static DeviceSummary? Map(UserDevice? d) =>
        d is null
            ? null
            : new DeviceSummary(d.Id, d.DeviceId, d.DeviceName, d.Os, d.Status.ToSnakeCase(), d.LastUsedAt, d.CreatedAt);
}
