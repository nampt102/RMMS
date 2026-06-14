using FluentAssertions;
using Rmms.Application.Dashboard;
using Rmms.Domain.Approvals;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Dashboard;

public sealed class DashboardSummaryHandlerTests
{
    // TestClock default 2026-06-01 09:00 UTC → VN (+7) date = 2026-06-01.
    private static readonly DateTimeOffset TodayCheckIn = new(2026, 6, 1, 2, 0, 0, TimeSpan.Zero); // 09:00 VN

    private static User Pg(string email) => UserFactory.CreateActivePg(email);

    private static AttendanceCheckInData CheckInData(
        Guid userId,
        decimal distance = 10m,
        FaceVerificationResult face = FaceVerificationResult.Success,
        bool fakeGps = false,
        bool late = false) =>
        new(userId, Guid.NewGuid(), Guid.NewGuid(), TodayCheckIn,
            Latitude: 10.0m, Longitude: 106.0m, DistanceMeters: distance,
            FakeGpsDetected: fakeGps, FaceResult: face, FaceConfidence: 0.99m,
            SelfieUrl: null, StorePhotoUrl: null, IsLate: late, Note: null);

    [Fact]
    public async Task CountsPresenceBuckets_Online_CheckedOut_PendingReview_Anomaly()
    {
        await using var db = TestDbContextFactory.Create();

        var working = Pg("working@x.io");      // valid, no checkout → online
        var done = Pg("done@x.io");            // valid + checkout → checked out
        var review = Pg("review@x.io");        // face fail → pending review
        var fake = Pg("fake@x.io");            // fake gps → anomaly, not a presence bucket
        db.Users.AddRange(working, done, review, fake);

        db.AttendanceRecords.Add(AttendanceRecord.CheckIn(CheckInData(working.Id)));

        var doneRec = AttendanceRecord.CheckIn(CheckInData(done.Id));
        doneRec.CheckOut(new AttendanceCheckOutData(
            TodayCheckIn.AddHours(8), 10.0m, 106.0m, 10m,
            FaceVerificationResult.Success, 0.99m, null, null, null));
        db.AttendanceRecords.Add(doneRec);

        db.AttendanceRecords.Add(AttendanceRecord.CheckIn(
            CheckInData(review.Id, face: FaceVerificationResult.Fail)));
        db.AttendanceRecords.Add(AttendanceRecord.CheckIn(
            CheckInData(fake.Id, fakeGps: true)));
        await db.SaveChangesAsync();

        var res = await new GetDashboardSummaryQueryHandler(db, new TestClock())
            .Handle(new GetDashboardSummaryQuery(Guid.NewGuid(), UserRole.Admin), default);

        res.IsSuccess.Should().BeTrue();
        var s = res.Value;
        s.TotalMembers.Should().Be(4);
        s.Online.Should().Be(1);
        s.CheckedOutToday.Should().Be(1);
        s.PendingReviewAttendance.Should().Be(1);  // the face-fail record
        s.AnomaliesToday.Should().Be(2);           // face fail + fake gps
    }

    [Fact]
    public async Task Leader_ScopesToManagedPgs_AndOwnApprovalQueue()
    {
        await using var db = TestDbContextFactory.Create();
        var leader = User.CreateByAdmin("lead@x.io", "plain:Pw123456", "Leader", UserRole.Leader, null, "vi");
        var mine = Pg("mine@x.io");
        var other = Pg("other@x.io");
        db.Users.AddRange(leader, mine, other);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(mine.Id, leader.Id, new DateOnly(2026, 5, 1)));

        // A pending approval routed to this leader (counts) and one to someone else (excluded).
        db.Approvals.Add(Approval.Create(ApprovalEntityType.LeaveRequest, Guid.NewGuid(), mine.Id, leader.Id, UserRole.Leader));
        db.Approvals.Add(Approval.Create(ApprovalEntityType.LeaveRequest, Guid.NewGuid(), other.Id, Guid.NewGuid(), UserRole.Leader));
        await db.SaveChangesAsync();

        var res = await new GetDashboardSummaryQueryHandler(db, new TestClock())
            .Handle(new GetDashboardSummaryQuery(leader.Id, UserRole.Leader), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.TotalMembers.Should().Be(1);       // only managed PG
        res.Value.PendingApprovals.Should().Be(1);   // only the leader's queue
    }
}
