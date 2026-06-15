using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Application.Reports;

// ===== Leave requests export (M15) =====

public sealed record ExportLeaveRequestsCommand(string? Status) : IRequest<Result<ReportFile>>;

internal sealed class ExportLeaveRequestsCommandHandler : IRequestHandler<ExportLeaveRequestsCommand, Result<ReportFile>>
{
    private const int MaxRows = 20_000;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly IAppDbContext _db;
    private readonly IReportExporter _exporter;

    public ExportLeaveRequestsCommandHandler(IAppDbContext db, IReportExporter exporter)
    {
        _db = db;
        _exporter = exporter;
    }

    public async ValueTask<Result<ReportFile>> Handle(ExportLeaveRequestsCommand command, CancellationToken ct)
    {
        var q = _db.LeaveRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(command.Status) && Enum.TryParse<RequestStatus>(command.Status, ignoreCase: true, out var s))
        {
            q = q.Where(r => r.Status == s);
        }

        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(MaxRows)
            .Join(_db.Users.AsNoTracking(), r => r.UserId, u => u.Id, (r, u) => new { r, u.FullName })
            .ToListAsync(ct);

        var headers = new[] { "Requester", "Type", "From", "To", "Start", "End", "Status", "Reason", "Created" };
        var data = rows.Select(x => (IReadOnlyList<string>)new[]
        {
            x.FullName,
            x.r.LeaveType.ToSnakeCase(),
            x.r.StartDate.ToString("yyyy-MM-dd", Inv),
            x.r.EndDate.ToString("yyyy-MM-dd", Inv),
            x.r.StartTime?.ToString("HH:mm", Inv) ?? "",
            x.r.EndTime?.ToString("HH:mm", Inv) ?? "",
            x.r.Status.ToSnakeCase(),
            x.r.Reason,
            x.r.CreatedAt.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd HH:mm", Inv),
        }).ToList();

        return Result.Success(_exporter.ToXlsx(new ReportSheet("Leave", headers, data), "LeaveRequests"));
    }
}

// ===== OT requests export (M15) =====

public sealed record ExportOtRequestsCommand(string? Status) : IRequest<Result<ReportFile>>;

internal sealed class ExportOtRequestsCommandHandler : IRequestHandler<ExportOtRequestsCommand, Result<ReportFile>>
{
    private const int MaxRows = 20_000;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly IAppDbContext _db;
    private readonly IReportExporter _exporter;

    public ExportOtRequestsCommandHandler(IAppDbContext db, IReportExporter exporter)
    {
        _db = db;
        _exporter = exporter;
    }

    public async ValueTask<Result<ReportFile>> Handle(ExportOtRequestsCommand command, CancellationToken ct)
    {
        var q = _db.OtRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(command.Status) && Enum.TryParse<RequestStatus>(command.Status, ignoreCase: true, out var s))
        {
            q = q.Where(r => r.Status == s);
        }

        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(MaxRows)
            .Join(_db.Users.AsNoTracking(), r => r.UserId, u => u.Id, (r, u) => new { r, u.FullName })
            .ToListAsync(ct);

        var headers = new[] { "Requester", "Date", "Start", "End", "Status", "Reason", "Created" };
        var data = rows.Select(x => (IReadOnlyList<string>)new[]
        {
            x.FullName,
            x.r.OtDate.ToString("yyyy-MM-dd", Inv),
            x.r.StartTime.ToString("HH:mm", Inv),
            x.r.EndTime.ToString("HH:mm", Inv),
            x.r.Status.ToSnakeCase(),
            x.r.Reason,
            x.r.CreatedAt.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd HH:mm", Inv),
        }).ToList();

        return Result.Success(_exporter.ToXlsx(new ReportSheet("OT", headers, data), "OtRequests"));
    }
}
