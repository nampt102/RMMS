using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rmms.Application.Maintenance;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Attendance;

public sealed class AttendancePhotoRetentionServiceTests
{
    private static AttendanceRecord RecordWithPhotos(DateTimeOffset checkInAt)
    {
        return AttendanceRecord.CheckIn(new AttendanceCheckInData(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), checkInAt,
            1m, 1m, 10m, FakeGpsDetected: false,
            FaceVerificationResult.Success, 0.99m,
            "key/selfie", "key/store", IsLate: false, Note: null));
    }

    [Fact]
    public async Task Purges_PhotosOlderThan90Days_KeepsRecent()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock(); // 2026-06-01 09:00Z

        var old = RecordWithPhotos(clock.UtcNow.AddDays(-100));
        var recent = RecordWithPhotos(clock.UtcNow.AddDays(-10));
        db.AttendanceRecords.Add(old);
        db.AttendanceRecords.Add(recent);
        await db.SaveChangesAsync();

        var purged = await new AttendancePhotoRetentionService(
                db, new FakePhotoStorage(), clock,
                NullLogger<AttendancePhotoRetentionService>.Instance)
            .RunAsync(default);

        purged.Should().Be(1);
        db.AttendanceRecords.Single(a => a.Id == old.Id).CheckInSelfieUrl.Should().BeNull();
        db.AttendanceRecords.Single(a => a.Id == old.Id).CheckInStorePhotoUrl.Should().BeNull();
        db.AttendanceRecords.Single(a => a.Id == recent.Id).CheckInSelfieUrl.Should().Be("key/selfie");
    }
}
