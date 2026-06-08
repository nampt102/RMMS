using FluentAssertions;
using Rmms.Application.Approvals;
using Rmms.Application.LeaveOt;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.LeaveOt;

public sealed class LeaveOtHandlerTests
{
    private static User Leader(string email = "leader@example.com") =>
        User.CreateByAdmin(email, "plain:Pw123456", "Leader", UserRole.Leader, null, "vi");

    // ----- Create + routing -----

    [Fact]
    public async Task CreateLeave_PgWithLeader_RoutesAndLinksApproval()
    {
        await using var db = TestDbContextFactory.Create();
        var approvals = new FakeApprovalService();
        var pg = UserFactory.CreateActivePg();
        var leader = Leader();
        db.Users.AddRange(pg, leader);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pg.Id, leader.Id, new DateOnly(2026, 6, 1)));
        await db.SaveChangesAsync();

        var result = await new CreateLeaveRequestCommandHandler(db, approvals, new InMemoryAuditLogger())
            .Handle(new CreateLeaveRequestCommand(pg.Id, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), null, null, "Việc gia đình"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApprovalId.Should().NotBeNull();
        approvals.Calls.Should().ContainSingle(c => c.EntityType == ApprovalEntityType.LeaveRequest && c.ApproverId == leader.Id);
        db.LeaveRequests.Single(r => r.Id == result.Value.Id).Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task CreateOt_NoLeader_StillCreatesPending()
    {
        await using var db = TestDbContextFactory.Create();
        var approvals = new FakeApprovalService();

        var result = await new CreateOtRequestCommandHandler(db, approvals, new InMemoryAuditLogger())
            .Handle(new CreateOtRequestCommand(Guid.NewGuid(), new DateOnly(2026, 7, 1), new TimeOnly(18, 0), new TimeOnly(20, 0), "Chốt số"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("pending");
        approvals.Calls.Should().BeEmpty(); // unknown owner → no routing
    }

    [Fact]
    public async Task CreateEmergency_NoOpenAttendance_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await new CreateEmergencyLeaveCommandHandler(db, new FakeApprovalService(), new InMemoryAuditLogger(), new TestClock())
            .Handle(new CreateEmergencyLeaveCommand(Guid.NewGuid(), "Ốm đột xuất"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NoOpenAttendance);
    }

    // ----- Actuation via M09 (ApprovalActuation) -----

    [Fact]
    public async Task ApproveApproval_ForLeaveRequest_ActuatesLeave()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leaderId = Guid.NewGuid();
        var pgId = Guid.NewGuid();
        var leave = LeaveRequest.Create(pgId, LeaveType.Regular, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1), null, null, "x");
        db.LeaveRequests.Add(leave);
        var approval = Approval.Create(ApprovalEntityType.LeaveRequest, leave.Id, pgId, leaderId, UserRole.Leader);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(approval.Id, leaderId, ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        db.LeaveRequests.Single(r => r.Id == leave.Id).Status.Should().Be(RequestStatus.Approved);
    }

    [Fact]
    public async Task RejectApproval_ForOtRequest_ActuatesOt()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leaderId = Guid.NewGuid();
        var pgId = Guid.NewGuid();
        var ot = OtRequest.Create(pgId, new DateOnly(2026, 7, 1), new TimeOnly(18, 0), new TimeOnly(20, 0), "x");
        db.OtRequests.Add(ot);
        var approval = Approval.Create(ApprovalEntityType.OtRequest, ot.Id, pgId, leaderId, UserRole.Leader);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new RejectApprovalCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeNotificationService())
            .Handle(new RejectApprovalCommand(approval.Id, leaderId, "Không cần OT", ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        db.OtRequests.Single(r => r.Id == ot.Id).Status.Should().Be(RequestStatus.Rejected);
    }
}
