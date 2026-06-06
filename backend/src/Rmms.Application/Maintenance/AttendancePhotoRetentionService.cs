using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;

namespace Rmms.Application.Maintenance;

/// <inheritdoc cref="IAttendancePhotoRetentionService" />
internal sealed class AttendancePhotoRetentionService : IAttendancePhotoRetentionService
{
    /// <summary>CR-4: selfies / store photos are retained for 90 days, then auto-deleted.</summary>
    private const int RetentionDays = 90;

    /// <summary>Cap rows per run so a daily job stays bounded even with a large backlog.</summary>
    private const int BatchSize = 500;

    private readonly IAppDbContext _db;
    private readonly IAttendancePhotoStorage _photos;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AttendancePhotoRetentionService> _logger;

    public AttendancePhotoRetentionService(
        IAppDbContext db,
        IAttendancePhotoStorage photos,
        IDateTimeProvider clock,
        ILogger<AttendancePhotoRetentionService> logger)
    {
        _db = db;
        _photos = photos;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var cutoff = _clock.UtcNow.AddDays(-RetentionDays);

        var stale = await _db.AttendanceRecords
            .Where(a => a.CheckInAt < cutoff && (
                a.CheckInSelfieUrl != null || a.CheckInStorePhotoUrl != null ||
                a.CheckOutSelfieUrl != null || a.CheckOutStorePhotoUrl != null))
            .OrderBy(a => a.CheckInAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var r in stale)
        {
            await _photos.DeleteAsync(r.CheckInSelfieUrl, ct);
            await _photos.DeleteAsync(r.CheckInStorePhotoUrl, ct);
            await _photos.DeleteAsync(r.CheckOutSelfieUrl, ct);
            await _photos.DeleteAsync(r.CheckOutStorePhotoUrl, ct);
            r.PurgePhotos();
        }

        if (stale.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Attendance photo retention purged photos for {Count} record(s) older than {Days}d.",
                stale.Count, RetentionDays);
        }

        return stale.Count;
    }
}
