using FluentAssertions;
using Microsoft.Extensions.Options;
using Rmms.Application.Approvals;
using Rmms.Application.Common.Options;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.Scheduling;
using Rmms.Infrastructure.Approvals;
using Rmms.Infrastructure.Email;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Approvals;

public sealed class ApprovalHandlerTests
{
    private static ApprovalTokenService Tokens() =>
        new(Options.Create(new ApprovalOptions { SigningKey = "unit-test-signing-key", TokenTtlHours = 24 }));

    private static Approval SeedPending(AppDbContext db, Guid requesterId, Guid approverId, UserRole role = UserRole.Leader)
    {
        var a = Approval.Create(ApprovalEntityType.WorkSchedule, Guid.NewGuid(), requesterId, approverId, role);
        db.Approvals.Add(a);
        return a;
    }

    // ---------- Approve / Reject ----------

    [Fact]
    public async Task Approve_ByApprover_Succeeds_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var approverId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), approverId);
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, audit, clock, new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(a.Id, approverId, ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        db.Approvals.Single(x => x.Id == a.Id).Status.Should().Be(ApprovalStatus.Approved);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.ApprovalApproved);
    }

    [Fact]
    public async Task Approve_ByNonApprover_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var a = SeedPending(db, Guid.NewGuid(), Guid.NewGuid());
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, new InMemoryAuditLogger(), new TestClock(), new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(a.Id, Guid.NewGuid(), ApprovalDecisionVia.App), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotApprover);
    }

    [Fact]
    public async Task Approve_WhenNotPending_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var approverId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), approverId);
        a.Approve(approverId, ApprovalDecisionVia.App, clock.UtcNow);
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(a.Id, approverId, ApprovalDecisionVia.App), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ApprovalNotPending);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsRejectReasonRequired()
    {
        await using var db = TestDbContextFactory.Create();
        var approverId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), approverId);
        await db.SaveChangesAsync();

        var result = await new RejectApprovalCommandHandler(db, new InMemoryAuditLogger(), new TestClock(), new FakeNotificationService())
            .Handle(new RejectApprovalCommand(a.Id, approverId, "  ", ApprovalDecisionVia.App), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RejectReasonRequired);
    }

    [Fact]
    public async Task Reject_WithReason_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var approverId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), approverId);
        await db.SaveChangesAsync();

        var result = await new RejectApprovalCommandHandler(db, new InMemoryAuditLogger(), new TestClock(), new FakeNotificationService())
            .Handle(new RejectApprovalCommand(a.Id, approverId, "Sai ca", ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Approvals.Single(x => x.Id == a.Id);
        saved.Status.Should().Be(ApprovalStatus.Rejected);
        saved.DecisionReason.Should().Be("Sai ca");
    }

    // ---------- Override (BR-408) ----------

    [Fact]
    public async Task Override_SetsOverridden_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var audit = new InMemoryAuditLogger();
        var adminId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), Guid.NewGuid());
        await db.SaveChangesAsync();

        var result = await new OverrideApprovalCommandHandler(db, audit, new TestClock())
            .Handle(new OverrideApprovalCommand(a.Id, adminId, "Quyết định của BGĐ"), default);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Approvals.Single(x => x.Id == a.Id);
        saved.Status.Should().Be(ApprovalStatus.Overridden);
        saved.OverriddenBy.Should().Be(adminId);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.ApprovalOverridden);
    }

    [Fact]
    public async Task Override_WhenAlreadyOverridden_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var a = SeedPending(db, Guid.NewGuid(), Guid.NewGuid());
        a.Override(Guid.NewGuid(), "first", clock.UtcNow);
        await db.SaveChangesAsync();

        var result = await new OverrideApprovalCommandHandler(db, new InMemoryAuditLogger(), clock)
            .Handle(new OverrideApprovalCommand(a.Id, Guid.NewGuid(), "second"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    // ---------- Queries ----------

    [Fact]
    public async Task GetPending_ReturnsOnlyApproversPending_WithRequesterName()
    {
        await using var db = TestDbContextFactory.Create();
        var approverId = Guid.NewGuid();
        var requester = UserFactory.CreateActivePg();
        db.Users.Add(requester);
        SeedPending(db, requester.Id, approverId);
        SeedPending(db, requester.Id, Guid.NewGuid()); // someone else's queue
        await db.SaveChangesAsync();

        var result = await new GetPendingApprovalsQueryHandler(db)
            .Handle(new GetPendingApprovalsQuery(approverId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].RequesterName.Should().Be(requester.FullName);
    }

    [Fact]
    public async Task GetDetail_Stranger_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var a = SeedPending(db, Guid.NewGuid(), Guid.NewGuid());
        await db.SaveChangesAsync();

        var result = await new GetApprovalDetailQueryHandler(db)
            .Handle(new GetApprovalDetailQuery(a.Id, Guid.NewGuid(), IsAdmin: false), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PermissionDenied);
    }

    // ---------- Token service ----------

    [Fact]
    public void Token_IssueThenVerify_RoundTrips()
    {
        var svc = Tokens();
        var now = new DateTimeOffset(2026, 6, 7, 9, 0, 0, TimeSpan.Zero);
        var aid = Guid.NewGuid();
        var uid = Guid.NewGuid();

        var issued = svc.Issue(aid, uid, new[] { "approve", "reject" }, now);
        var payload = svc.Verify(issued.Token, now);

        payload.Should().NotBeNull();
        payload!.ApprovalId.Should().Be(aid);
        payload.ApproverId.Should().Be(uid);
        payload.ActionOptions.Should().BeEquivalentTo(new[] { "approve", "reject" });
    }

    [Fact]
    public void Token_Tampered_ReturnsNull()
    {
        var svc = Tokens();
        var now = new DateTimeOffset(2026, 6, 7, 9, 0, 0, TimeSpan.Zero);
        var issued = svc.Issue(Guid.NewGuid(), Guid.NewGuid(), new[] { "approve" }, now);

        var tampered = issued.Token[..^2] + (issued.Token[^1] == 'a' ? "bb" : "aa");
        svc.Verify(tampered, now).Should().BeNull();
    }

    [Fact]
    public void Token_Expired_ReturnsNull()
    {
        var svc = Tokens();
        var now = new DateTimeOffset(2026, 6, 7, 9, 0, 0, TimeSpan.Zero);
        var issued = svc.Issue(Guid.NewGuid(), Guid.NewGuid(), new[] { "approve" }, now);

        svc.Verify(issued.Token, now.AddHours(25)).Should().BeNull();
    }

    // ---------- Email-link flow (AC-18) ----------

    private static async Task<(Guid approvalId, string token)> SeedBuhTokenAsync(AppDbContext db, ApprovalTokenService tokens, TestClock clock, TimeSpan lifetime)
    {
        var buhId = Guid.NewGuid();
        var a = SeedPending(db, Guid.NewGuid(), buhId, UserRole.Buh);
        var issued = tokens.Issue(a.Id, buhId, new[] { "approve", "reject" }, clock.UtcNow);
        db.ApprovalEmailTokens.Add(ApprovalEmailToken.Issue(a.Id, issued.Hash, clock.UtcNow, lifetime));
        await db.SaveChangesAsync();
        return (a.Id, issued.Token);
    }

    [Fact]
    public async Task EmailConfirm_Approve_Decides_AndConsumesToken()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var tokens = Tokens();
        var (approvalId, token) = await SeedBuhTokenAsync(db, tokens, clock, TimeSpan.FromHours(24));

        var handler = new EmailActionConfirmCommandHandler(db, tokens, new InMemoryAuditLogger(), clock, new FakeNotificationService());
        var result = await handler.Handle(new EmailActionConfirmCommand(token, "approve", null, "1.2.3.4", "UA"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("approved");
        db.Approvals.Single(x => x.Id == approvalId).Status.Should().Be(ApprovalStatus.Approved);
        db.ApprovalEmailTokens.Single(t => t.ApprovalId == approvalId).IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task EmailConfirm_SecondUse_ReturnsUsed()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var tokens = Tokens();
        var (_, token) = await SeedBuhTokenAsync(db, tokens, clock, TimeSpan.FromHours(24));
        var handler = new EmailActionConfirmCommandHandler(db, tokens, new InMemoryAuditLogger(), clock, new FakeNotificationService());

        await handler.Handle(new EmailActionConfirmCommand(token, "approve", null, null, null), default);
        var second = await handler.Handle(new EmailActionConfirmCommand(token, "approve", null, null, null), default);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(ErrorCodes.EmailTokenUsed);
    }

    [Fact]
    public async Task EmailConfirm_Expired_ReturnsExpired()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var tokens = Tokens();
        var (_, token) = await SeedBuhTokenAsync(db, tokens, clock, TimeSpan.FromHours(-1)); // already expired row
        var handler = new EmailActionConfirmCommandHandler(db, tokens, new InMemoryAuditLogger(), clock, new FakeNotificationService());

        var result = await handler.Handle(new EmailActionConfirmCommand(token, "approve", null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.EmailTokenExpired);
    }

    [Fact]
    public async Task EmailPreview_ValidPending_ReturnsValid()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var tokens = Tokens();
        var (_, token) = await SeedBuhTokenAsync(db, tokens, clock, TimeSpan.FromHours(24));

        var result = await new EmailActionPreviewQueryHandler(db, tokens, clock)
            .Handle(new EmailActionPreviewQuery(token), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Valid.Should().BeTrue();
        result.Value.Used.Should().BeFalse();
        result.Value.EntityType.Should().Be("work_schedule");
    }

    // ---------- Schedule wiring (M09 ↔ M07) ----------

    private static WorkSchedule SeedPendingSchedule(AppDbContext db, Guid pgId)
    {
        var schedule = WorkSchedule.Create(
            pgId,
            new DateOnly(2026, 7, 1),
            new[] { new ScheduleShiftInput(Guid.NewGuid(), new TimeOnly(8, 0), new TimeOnly(17, 0)) });
        db.WorkSchedules.Add(schedule);
        return schedule;
    }

    [Fact]
    public async Task ApproveApproval_ForWorkSchedule_ActuatesSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leaderId = Guid.NewGuid();
        var pgId = Guid.NewGuid();
        var schedule = SeedPendingSchedule(db, pgId);
        var approval = Approval.Create(ApprovalEntityType.WorkSchedule, schedule.Id, pgId, leaderId, UserRole.Leader);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(approval.Id, leaderId, ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        db.Approvals.Single(a => a.Id == approval.Id).Status.Should().Be(ApprovalStatus.Approved);
        db.WorkSchedules.Single(s => s.Id == schedule.Id).Status.Should().Be(WorkScheduleStatus.Approved);
    }

    [Fact]
    public async Task RejectApproval_ForWorkSchedule_ActuatesSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var leaderId = Guid.NewGuid();
        var pgId = Guid.NewGuid();
        var schedule = SeedPendingSchedule(db, pgId);
        var approval = Approval.Create(ApprovalEntityType.WorkSchedule, schedule.Id, pgId, leaderId, UserRole.Leader);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new RejectApprovalCommandHandler(db, new InMemoryAuditLogger(), clock, new FakeNotificationService())
            .Handle(new RejectApprovalCommand(approval.Id, leaderId, "Trùng ca", ApprovalDecisionVia.App), default);

        result.IsSuccess.Should().BeTrue();
        var s = db.WorkSchedules.Single(x => x.Id == schedule.Id);
        s.Status.Should().Be(WorkScheduleStatus.Rejected);
        s.RejectReason.Should().Be("Trùng ca");
    }

    // ---------- Producer (IApprovalService) ----------

    [Fact]
    public async Task CreateForBuh_IssuesToken_AndSendsEmail()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var tokens = Tokens();
        var emailSender = new CapturingEmailSender();
        var buh = UserFactory.CreateAdmin("buh@example.com"); // any user row to resolve email/name
        db.Users.Add(buh);
        await db.SaveChangesAsync();

        var svc = new ApprovalService(
            db, tokens, emailSender, new FakeNotificationService(), clock,
            Options.Create(new ApprovalOptions { SigningKey = "k", WebApprovalPath = "/approve" }),
            Options.Create(new AppUrlOptions { AppBaseUrl = "http://localhost:3000" }));

        var id = await svc.CreateAsync(ApprovalEntityType.WorkSchedule, Guid.NewGuid(), Guid.NewGuid(), buh.Id, UserRole.Buh);
        await db.SaveChangesAsync();

        db.Approvals.Single(x => x.Id == id).ApproverRole.Should().Be(UserRole.Buh);
        db.ApprovalEmailTokens.Should().ContainSingle(t => t.ApprovalId == id);
        emailSender.Sent.Should().ContainSingle()
            .Which.BodyText.Should().Contain("/approve?token=");
    }
}
