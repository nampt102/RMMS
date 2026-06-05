using FluentAssertions;
using Rmms.Application.Devices.ApproveDevice;
using Rmms.Application.Devices.RejectDevice;
using Rmms.Domain.Auth;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Devices;

public sealed class DeviceApprovalHandlerTests
{
    private static UserDevice SeedActive(AppDbContext db, Guid userId, string deviceId, DateTimeOffset at)
    {
        var d = UserDevice.RegisterFirstActive(userId, deviceId, "Old Phone", "ios", "16.0", "1.0.0", null, at);
        db.UserDevices.Add(d);
        return d;
    }

    private static UserDevice SeedPending(AppDbContext db, Guid userId, string deviceId)
    {
        var d = UserDevice.RegisterPendingApproval(userId, deviceId, "New Phone", "android", "14", "1.0.0", null);
        db.UserDevices.Add(d);
        return d;
    }

    [Fact]
    public async Task Approve_ActivatesNewDevice_ReplacesOld_AndRevokesOldTokens()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var userId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var oldDevice = SeedActive(db, userId, "dev-old", clock.UtcNow);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(RefreshToken.Issue(userId, oldDevice.Id, "hash:old", clock.UtcNow, TimeSpan.FromDays(30)));
        var pending = SeedPending(db, userId, "dev-new");
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var result = await new ApproveDeviceCommandHandler(db, audit, clock)
            .Handle(new ApproveDeviceCommand(pending.Id, approverId, ApproverIsAdmin: true), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Single(d => d.DeviceId == "dev-new").Status.Should().Be(DeviceStatus.Active);
        db.UserDevices.Single(d => d.DeviceId == "dev-old").Status.Should().Be(DeviceStatus.Replaced);
        db.RefreshTokens.Single().RevokedAt.Should().NotBeNull("the old device's tokens are revoked");
        audit.Calls.Should().Contain(c => c.Action == AuditAction.DeviceApproved);
    }

    [Fact]
    public async Task Approve_UnknownDevice_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await new ApproveDeviceCommandHandler(db, new InMemoryAuditLogger(), new TestClock())
            .Handle(new ApproveDeviceCommand(Guid.NewGuid(), Guid.NewGuid(), ApproverIsAdmin: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Approve_AlreadyActiveDevice_ReturnsApprovalNotPending()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var userId = Guid.NewGuid();
        var active = SeedActive(db, userId, "dev-1", clock.UtcNow);
        await db.SaveChangesAsync();

        var result = await new ApproveDeviceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveDeviceCommand(active.Id, Guid.NewGuid(), ApproverIsAdmin: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ApprovalNotPending);
    }

    [Fact]
    public async Task Reject_PendingDevice_SetsRejected_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var userId = Guid.NewGuid();
        var pending = SeedPending(db, userId, "dev-new");
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditLogger();
        var result = await new RejectDeviceCommandHandler(db, audit, clock)
            .Handle(new RejectDeviceCommand(pending.Id, Guid.NewGuid(), "Không nhận ra thiết bị này", ApproverIsAdmin: true), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Single().Status.Should().Be(DeviceStatus.Rejected);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.DeviceRejected);
    }

    [Fact]
    public async Task Reject_NonPendingDevice_ReturnsApprovalNotPending()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var active = SeedActive(db, Guid.NewGuid(), "dev-1", clock.UtcNow);
        await db.SaveChangesAsync();

        var result = await new RejectDeviceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new RejectDeviceCommand(active.Id, Guid.NewGuid(), "reason", ApproverIsAdmin: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ApprovalNotPending);
    }

    [Fact]
    public async Task Approve_AsManagingLeader_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leader = User.CreateByAdmin("leader@example.com", "plain:Pwd12345", "Leader", UserRole.Leader, null, "vi");
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.AddRange(leader, pg);
        db.UserLeaderAssignments.Add(UserLeaderAssignment.Create(pg.Id, leader.Id, new DateOnly(2026, 6, 1)));
        var pending = SeedPending(db, pg.Id, "dev-new");
        await db.SaveChangesAsync();

        var result = await new ApproveDeviceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveDeviceCommand(pending.Id, leader.Id, ApproverIsAdmin: false), default);

        result.IsSuccess.Should().BeTrue();
        db.UserDevices.Single(d => d.DeviceId == "dev-new").Status.Should().Be(DeviceStatus.Active);
    }

    [Fact]
    public async Task Approve_AsNonManagingLeader_ReturnsNotApprover()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leader = User.CreateByAdmin("leader@example.com", "plain:Pwd12345", "Leader", UserRole.Leader, null, "vi");
        var pg = UserFactory.CreateActivePg("pg@example.com"); // NOT managed by leader
        db.Users.AddRange(leader, pg);
        var pending = SeedPending(db, pg.Id, "dev-new");
        await db.SaveChangesAsync();

        var result = await new ApproveDeviceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new ApproveDeviceCommand(pending.Id, leader.Id, ApproverIsAdmin: false), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotApprover);
        db.UserDevices.Single().Status.Should().Be(DeviceStatus.PendingApproval, "rejected approval must not mutate state");
    }

    [Fact]
    public async Task Reject_AsNonManagingLeader_ReturnsNotApprover()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leader = User.CreateByAdmin("leader@example.com", "plain:Pwd12345", "Leader", UserRole.Leader, null, "vi");
        var pg = UserFactory.CreateActivePg("pg@example.com");
        db.Users.AddRange(leader, pg);
        var pending = SeedPending(db, pg.Id, "dev-new");
        await db.SaveChangesAsync();

        var result = await new RejectDeviceCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new RejectDeviceCommand(pending.Id, leader.Id, "no", ApproverIsAdmin: false), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotApprover);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectValidator_RequiresReason(string reason)
    {
        var r = new RejectDeviceCommandValidator().Validate(new RejectDeviceCommand(Guid.NewGuid(), Guid.NewGuid(), reason, ApproverIsAdmin: true));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorCode == "REJECT_REASON_REQUIRED");
    }
}
