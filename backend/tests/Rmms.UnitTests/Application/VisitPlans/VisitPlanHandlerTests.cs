using FluentAssertions;
using Rmms.Application.Approvals;
using Rmms.Application.Forms;
using Rmms.Application.VisitPlans;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Domain.Forms;
using Rmms.Domain.Organization;
using Rmms.Domain.Users;
using Rmms.Domain.VisitPlan;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.VisitPlans;

public sealed class VisitPlanHandlerTests
{
    private const string Schema =
        "{\"fields\":[{\"id\":\"q1\",\"type\":\"text\",\"label_vi\":\"Ghi chú\",\"label_en\":\"Note\",\"required\":false}],\"rules\":{}}";

    private static readonly TestClock Clock = new() { UtcNow = new DateTimeOffset(2026, 06, 14, 3, 0, 0, TimeSpan.Zero) };

    private static async Task<Guid> PublishedFormAsync(AppDbContext db, string code = "VR-1")
    {
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateFormCommand(code, "Báo cáo viếng thăm", "Visit Report", null, null, "visit_report", Schema), default);
        await new PublishFormCommandHandler(db, new InMemoryAuditLogger(), Clock, new TestCurrentUser { UserId = Guid.NewGuid() })
            .Handle(new PublishFormCommand(create.Value), default);
        return create.Value;
    }

    private static Guid SeedStore(AppDbContext db, string code = "ST-1")
    {
        var store = Store.Create(code, "Store " + code, "Addr", 10.0m, 106.0m, null);
        db.Stores.Add(store);
        return store.Id;
    }

    private static User SeedActiveBuh(AppDbContext db, string email = "buh@example.com")
    {
        var buh = User.CreateByAdmin(email, "plain:BuhPwd1", "BUH Test", UserRole.Buh);
        db.Users.Add(buh);
        return buh;
    }

    // ---------- Create + BUH routing ----------

    [Fact]
    public async Task Create_RoutesToBuh_AndLinksApproval()
    {
        await using var db = TestDbContextFactory.Create();
        var buh = SeedActiveBuh(db);
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var approvals = new FakeApprovalService();

        var result = await new CreateVisitPlanCommandHandler(db, approvals, new InMemoryAuditLogger())
            .Handle(new CreateVisitPlanCommand(leaderId, new DateOnly(2026, 6, 20), "Plan A",
                new[] { new VisitPlanItemInput(storeId, formId) }), default);

        result.IsSuccess.Should().BeTrue();
        approvals.Calls.Should().ContainSingle()
            .Which.Should().Match<FakeApprovalService.Call>(c =>
                c.EntityType == ApprovalEntityType.VisitPlan && c.ApproverId == buh.Id && c.ApproverRole == UserRole.Buh);
        var plan = db.VisitPlans.Single();
        plan.Status.Should().Be(VisitPlanStatus.Pending);
        plan.ApprovalId.Should().NotBeNull();
        plan.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_NoActiveBuh_StaysPending_NoApprovalRow()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var approvals = new FakeApprovalService();

        var result = await new CreateVisitPlanCommandHandler(db, approvals, new InMemoryAuditLogger())
            .Handle(new CreateVisitPlanCommand(Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(storeId, formId) }), default);

        result.IsSuccess.Should().BeTrue();
        approvals.Calls.Should().BeEmpty();
        db.VisitPlans.Single().ApprovalId.Should().BeNull();
    }

    [Fact]
    public async Task Create_StoreNotFound_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        SeedActiveBuh(db);
        var formId = await PublishedFormAsync(db);

        var result = await new CreateVisitPlanCommandHandler(db, new FakeApprovalService(), new InMemoryAuditLogger())
            .Handle(new CreateVisitPlanCommand(Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(Guid.NewGuid(), formId) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Create_UnpublishedForm_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        SeedActiveBuh(db);
        var storeId = SeedStore(db);
        // create draft (not published)
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateFormCommand("VR-DRAFT", "A", "A", null, null, "visit_report", Schema), default);

        var result = await new CreateVisitPlanCommandHandler(db, new FakeApprovalService(), new InMemoryAuditLogger())
            .Handle(new CreateVisitPlanCommand(Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(storeId, create.Value) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Create_DuplicateStore_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        SeedActiveBuh(db);
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);

        var result = await new CreateVisitPlanCommandHandler(db, new FakeApprovalService(), new InMemoryAuditLogger())
            .Handle(new CreateVisitPlanCommand(Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(storeId, formId), new VisitPlanItemInput(storeId, formId) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    // ---------- Edit ----------

    [Fact]
    public async Task Edit_Pending_ByOwner_ReplacesItems()
    {
        // Separate contexts per step mirror real per-request DbContext scoping (and avoid the
        // InMemory provider's quirk when the same context both adds and re-deletes child rows).
        var dbName = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid();
        Guid planId, store1, store2, formId;

        await using (var db = TestDbContextFactory.Create(dbName))
        {
            store1 = SeedStore(db, "ST-1");
            store2 = SeedStore(db, "ST-2");
            formId = await PublishedFormAsync(db);
            var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), "old",
                new[] { new VisitItemInput(store1, formId) });
            db.VisitPlans.Add(plan);
            await db.SaveChangesAsync();
            planId = plan.Id;
        }

        await using (var db = TestDbContextFactory.Create(dbName))
        {
            // Same-count replacement (swap the store) edits in place — mirrors the proven M07
            // pattern. Growing an owned collection trips the InMemory provider (not real Postgres).
            var result = await new EditVisitPlanCommandHandler(db, new InMemoryAuditLogger())
                .Handle(new EditVisitPlanCommand(planId, leaderId, new DateOnly(2026, 6, 20), "new",
                    new[] { new VisitPlanItemInput(store2, formId) }), default);
            result.IsSuccess.Should().BeTrue();
        }

        await using (var assertDb = TestDbContextFactory.Create(dbName))
        {
            var saved = assertDb.VisitPlans.Single(); // owned items auto-loaded
            saved.Notes.Should().Be("new");
            saved.Items.Should().ContainSingle();
            saved.Items[0].StoreId.Should().Be(store2);
        }
    }

    [Fact]
    public async Task Edit_ByNonOwner_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var plan = VisitPlan.Create(Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        db.VisitPlans.Add(plan);
        await db.SaveChangesAsync();

        var result = await new EditVisitPlanCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new EditVisitPlanCommand(plan.Id, Guid.NewGuid(), new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(storeId, formId) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PermissionDenied);
    }

    [Fact]
    public async Task Edit_WhenApproved_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        plan.Approve(Clock.UtcNow);
        db.VisitPlans.Add(plan);
        await db.SaveChangesAsync();

        var result = await new EditVisitPlanCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new EditVisitPlanCommand(plan.Id, leaderId, new DateOnly(2026, 6, 20), null,
                new[] { new VisitPlanItemInput(storeId, formId) }), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    // ---------- Approval actuation (M09 → M11) ----------

    [Fact]
    public async Task ApproveApproval_ForVisitPlan_ActuatesPlan()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var buhId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        db.VisitPlans.Add(plan);
        var approval = Approval.Create(ApprovalEntityType.VisitPlan, plan.Id, leaderId, buhId, UserRole.Buh);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new ApproveApprovalCommandHandler(db, new InMemoryAuditLogger(), Clock, new FakeNotificationService())
            .Handle(new ApproveApprovalCommand(approval.Id, buhId, ApprovalDecisionVia.Web), default);

        result.IsSuccess.Should().BeTrue();
        db.VisitPlans.Single(p => p.Id == plan.Id).Status.Should().Be(VisitPlanStatus.Approved);
    }

    [Fact]
    public async Task RejectApproval_ForVisitPlan_ActuatesPlan()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var buhId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        db.VisitPlans.Add(plan);
        var approval = Approval.Create(ApprovalEntityType.VisitPlan, plan.Id, leaderId, buhId, UserRole.Buh);
        db.Approvals.Add(approval);
        await db.SaveChangesAsync();

        var result = await new RejectApprovalCommandHandler(db, new InMemoryAuditLogger(), Clock, new FakeNotificationService())
            .Handle(new RejectApprovalCommand(approval.Id, buhId, "Không hợp lý", ApprovalDecisionVia.Web), default);

        result.IsSuccess.Should().BeTrue();
        db.VisitPlans.Single(p => p.Id == plan.Id).Status.Should().Be(VisitPlanStatus.Rejected);
    }

    // ---------- Execute (link post-visit submission) ----------

    private static FormSubmission SeedSubmission(AppDbContext db, Guid formId, Guid userId, Guid storeId)
    {
        var s = FormSubmission.Create(formId, Guid.NewGuid(), userId, storeId, "{}", null, null, 60, "key-" + Guid.NewGuid().ToString("N"), Clock.UtcNow);
        db.FormSubmissions.Add(s);
        return s;
    }

    [Fact]
    public async Task Execute_LinksSubmission_AndMarksExecutedWhenAllDone()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        plan.Approve(Clock.UtcNow);
        db.VisitPlans.Add(plan);
        var submission = SeedSubmission(db, formId, leaderId, storeId);
        await db.SaveChangesAsync();
        var itemId = plan.Items[0].Id;

        var result = await new ExecuteVisitItemCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new ExecuteVisitItemCommand(plan.Id, itemId, leaderId, submission.Id), default);

        result.IsSuccess.Should().BeTrue();
        var saved = db.VisitPlans.Single(p => p.Id == plan.Id);
        saved.Status.Should().Be(VisitPlanStatus.Executed);
        saved.Items[0].FormSubmissionId.Should().Be(submission.Id);
    }

    [Fact]
    public async Task Execute_WhenPlanNotApproved_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) }); // still pending
        db.VisitPlans.Add(plan);
        var submission = SeedSubmission(db, formId, leaderId, storeId);
        await db.SaveChangesAsync();

        var result = await new ExecuteVisitItemCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new ExecuteVisitItemCommand(plan.Id, plan.Items[0].Id, leaderId, submission.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Execute_SubmissionFormMismatch_ValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        plan.Approve(Clock.UtcNow);
        db.VisitPlans.Add(plan);
        var submission = SeedSubmission(db, Guid.NewGuid(), leaderId, storeId); // different form
        await db.SaveChangesAsync();

        var result = await new ExecuteVisitItemCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new ExecuteVisitItemCommand(plan.Id, plan.Items[0].Id, leaderId, submission.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Execute_SubmissionNotOwned_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var storeId = SeedStore(db);
        var formId = await PublishedFormAsync(db);
        var leaderId = Guid.NewGuid();
        var plan = VisitPlan.Create(leaderId, new DateOnly(2026, 6, 20), null,
            new[] { new VisitItemInput(storeId, formId) });
        plan.Approve(Clock.UtcNow);
        db.VisitPlans.Add(plan);
        var submission = SeedSubmission(db, formId, Guid.NewGuid(), storeId); // someone else's
        await db.SaveChangesAsync();

        var result = await new ExecuteVisitItemCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new ExecuteVisitItemCommand(plan.Id, plan.Items[0].Id, leaderId, submission.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PermissionDenied);
    }
}
