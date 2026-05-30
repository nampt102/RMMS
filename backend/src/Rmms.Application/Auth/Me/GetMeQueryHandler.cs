using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Shared.Errors;

namespace Rmms.Application.Auth.Me;

internal sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<MeDto>>
{
    private readonly IAppDbContext _db;

    public GetMeQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<MeDto>> Handle(GetMeQuery query, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user is null)
        {
            return Result.Failure<MeDto>(
                Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng."));
        }

        MeDeviceDto? device = null;
        if (query.DeviceId is { } deviceId)
        {
            device = await _db.UserDevices
                .AsNoTracking()
                .Where(d => d.Id == deviceId && d.UserId == user.Id)
                .Select(d => new MeDeviceDto(
                    d.Id,
                    d.DeviceId,
                    d.DeviceName,
                    d.Os,
                    d.OsVersion,
                    d.AppVersion,
                    d.Status.ToSnakeCase(),
                    d.LastUsedAt))
                .FirstOrDefaultAsync(ct);
        }

        return new MeDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.Role.ToSnakeCase(),
            user.Status.ToSnakeCase(),
            user.PreferredLanguage,
            user.EmailVerifiedAt,
            user.LastLoginAt,
            device);
    }
}
