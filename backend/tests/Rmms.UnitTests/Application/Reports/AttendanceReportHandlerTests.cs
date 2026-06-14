using FluentAssertions;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Reports;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Infrastructure.Persistence;
using Rmms.Infrastructure.Reports;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Reports;

public sealed class AttendanceReportHandlerTests
{
    private static AttendanceRecord Rec(
        Guid userId, Guid storeId, DateTimeOffset at,
        bool fakeGps = false, decimal distance = 10m, FaceVerificationResult face = FaceVerificationResult.Success, bool late = false)
        => AttendanceRecord.CheckIn(new AttendanceCheckInData(
            userId, Guid.NewGuid(), storeId, at, 10m, 106m, distance, fakeGps, face, 0.99m, "s", "p", late, null));

    private static (Guid userId, Guid storeId) SeedUserStore(AppDbContext db)
    {
        var user = UserFactory.CreateActivePg("rep@example.com");
        var store = Store.Create("ST-R", "Report Store", "Addr", 10m, 106m, null);
        db.Users.Add(user);
        db.Stores.Add(store);
        return (user.Id, store.Id);
    }

    [Fact]
    public async Task AttendanceReport_ReturnsRows_WithNames()
    {
        await using var db = TestDbContextFactory.Create();
        var (userId, storeId) = SeedUserStore(db);
        db.AttendanceRecords.Add(Rec(userId, storeId, new DateTimeOffset(2026, 6, 10, 3, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var res = await new GetAttendanceReportQueryHandler(db)
            .Handle(new GetAttendanceReportQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Should().ContainSingle();
        res.Value[0].UserName.Should().Be("PG Test");
        res.Value[0].StoreName.Should().Be("Report Store");
        res.Value[0].IsAnomaly.Should().BeFalse();
    }

    [Fact]
    public async Task AttendanceReport_AnomaliesOnly_ReturnsOnlyAnomalies()
    {
        await using var db = TestDbContextFactory.Create();
        var (userId, storeId) = SeedUserStore(db);
        db.AttendanceRecords.Add(Rec(userId, storeId, new DateTimeOffset(2026, 6, 10, 3, 0, 0, TimeSpan.Zero)));
        db.AttendanceRecords.Add(Rec(userId, storeId, new DateTimeOffset(2026, 6, 11, 3, 0, 0, TimeSpan.Zero), fakeGps: true));
        await db.SaveChangesAsync();

        var res = await new GetAttendanceReportQueryHandler(db)
            .Handle(new GetAttendanceReportQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), AnomaliesOnly: true), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Should().ContainSingle();
        res.Value[0].IsAnomaly.Should().BeTrue();
        res.Value[0].Status.Should().Be("fake_gps_blocked");
    }

    [Fact]
    public async Task AttendanceReport_InvalidRange_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var res = await new GetAttendanceReportQueryHandler(db)
            .Handle(new GetAttendanceReportQuery(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1)), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task AttendanceTrend_BucketsByDay()
    {
        await using var db = TestDbContextFactory.Create();
        var (userId, storeId) = SeedUserStore(db);
        var clock = new TestClock { UtcNow = new DateTimeOffset(2026, 6, 15, 3, 0, 0, TimeSpan.Zero) };
        // VN dates: 2026-06-15 (valid) and 2026-06-14 (anomaly via gps distance)
        db.AttendanceRecords.Add(Rec(userId, storeId, new DateTimeOffset(2026, 6, 15, 2, 0, 0, TimeSpan.Zero)));
        db.AttendanceRecords.Add(Rec(userId, storeId, new DateTimeOffset(2026, 6, 14, 2, 0, 0, TimeSpan.Zero), distance: 500m));
        await db.SaveChangesAsync();

        var res = await new GetAttendanceTrendQueryHandler(db, clock).Handle(new GetAttendanceTrendQuery(14), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Should().HaveCount(14);
        res.Value.Single(p => p.Date == new DateOnly(2026, 6, 15)).Valid.Should().Be(1);
        res.Value.Single(p => p.Date == new DateOnly(2026, 6, 14)).Anomaly.Should().Be(1);
    }

    [Fact]
    public void Exporter_ProducesXlsxBytes()
    {
        var exporter = new ClosedXmlReportExporter();
        var sheet = new ReportSheet(
            "Attendance",
            new[] { "Date", "User" },
            new List<IReadOnlyList<string>> { new[] { "2026-06-10", "PG Test" } });

        var file = exporter.ToXlsx(sheet, "Attendance_20260601_20260630");

        file.Content.Length.Should().BeGreaterThan(0);
        file.FileName.Should().EndWith(".xlsx");
        file.ContentType.Should().Contain("spreadsheetml");
    }
}
