namespace Rmms.Application.Common.Abstractions;

/// <summary>
/// Maintenance routine (M05, CR-4): purges attendance selfies + store photos older than the
/// 90-day retention window — deletes the object-storage copies and clears the URL columns,
/// while keeping the attendance record itself for compliance. Scheduled daily by the Hangfire
/// worker (<c>Rmms.Worker</c>).
/// </summary>
public interface IAttendancePhotoRetentionService
{
    /// <summary>Run one retention pass; returns the number of records whose photos were purged.</summary>
    Task<int> RunAsync(CancellationToken ct = default);
}
