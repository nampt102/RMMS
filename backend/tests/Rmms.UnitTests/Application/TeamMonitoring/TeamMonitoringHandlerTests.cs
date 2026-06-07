using FluentAssertions;
using Rmms.Application.TeamMonitoring;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Domain.Organization;
using Rmms.Domain.Scheduling;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.TeamMonitoring;

public sealed class TeamMonitoringHandlerTests
{
    // TestClock default 2026-06-01 09:00 UTC → VN (+7) date = 2026-06-01.
    private static readonly DateOnly Today = new(2026, 6, 1);

    private static User Pg(string email) => UserFactory.CreateActivePg(email);
    private static User Leader(string email) =>
        User.CreateByAdmin(email, "plain:Pw123456", "Leader", UserRole.Leader, null, "vi");

    private static WorkSchedule ApprovedScheduleToday(Guid userId)
    {
        var s = WorkSchedule.Create(userId, Today,
            new[] { new ScheduleShiftInput(Guid.NewGuid(), new TimeOnly(8, 0), new TimeOnly(17, 0)) });
        s.Submit(new DateTimeOffset(2026, 5, 30, 0, 0, 0, TimeSpan.Zero));
        s.Approve(Guid.NewGuid(), new DateTimeOffset(2026, 5, 30, 0, 0, 0, TimeSpan.Zero));
        return s;
    }

    [Fact]
    public async Task NoSchedule_NoAttendance_NoLeave_IsNoScheduleToday()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Pg("pg-a@x.io");
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var res = await new GetTeamTodayQueryHandler(db, new TestClock())
            .Handle(new GetTeamTodayQuery(Guid.NewGuid(), UserRole.Admin), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Members.Single(m => m.UserId == pg.Id).Status.Should().Be(TeamStatus.NoScheduleToday);
    }

    [Fact]
    public async Task ScheduledToday_NoAttendance_IsNotCheckedIn()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Pg("pg-b@x.io");
        db.Users.Add(pg);
        db.WorkSchedules.Add(ApprovedScheduleToday(pg.Id));
        await db.SaveChangesAsync();

        var res = await new GetTeamTodayQueryHandler(db, new TestClock())
            .Handle(new GetTeamTodayQuery(Guid.NewGuid(), UserRole.Admin), default);

        res.Value.Members.Single(m => m.UserId == pg.Id).Status.Should().Be(TeamStatus.NotCheckedIn);
    }

    [Fact]
    public async Task ApprovedLeaveCoveringToday_IsOnLeave()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Pg("pg-c@x.io");
        db.Users.Add(pg);
        db.WorkSchedules.Add(ApprovedScheduleToday(pg.Id)); // leave wins over schedule
        var leave = LeaveRequest.Create(pg.Id, LeaveType.Regular, Today, Today, null, null, "Ốm");
        leave.Approve(new DateTimeOffset(2026, 5, 31, 0, 0, 0, TimeSpan.Zero));
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var res = await new GetTeamTodayQueryHandler(db, new TestClock())
            .Handle(new GetTeamTodayQuery(Guid.NewGuid(), UserRole.Admin), default);

        res.Value.Members.Single(m => m.UserId == pg.Id).Status.Should().Be(TeamStatus.OnLeave);
    }

    [Fact]
    public async Task Leader_SeesOnlyManagedPgs()
    {
        await using var db = TestDbContextFactory.Create();
        var leader = Leader("lead@x.io");
        var mine = Pg("mine@x.io");
        var other = Pg("other@x.io");
        db.Users.AddRange(leader, mine, other);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(mine.Id, leader.Id, new DateOnly(2026, 5, 1)));
        await db.SaveChangesAsync();

        var res = await new GetTeamTodayQueryHandler(db, new TestClock())
            .Handle(new GetTeamTodayQuery(leader.Id, UserRole.Leader), default);

        res.Value.Members.Select(m => m.UserId).Should().BeEquivalentTo(new[] { mine.Id });
        res.Value.Summary[TeamStatus.NoScheduleToday].Should().Be(1);
    }
}
