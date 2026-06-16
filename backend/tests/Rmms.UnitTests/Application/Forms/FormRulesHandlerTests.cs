using FluentAssertions;
using Rmms.Application.Forms;
using Rmms.Domain.Attendance;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Forms;

/// <summary>M10 form-level rule enforcement on submit (gps_required / photo_required / require_check_in).</summary>
public sealed class FormRulesHandlerTests
{
    private static readonly TestClock Clock = new() { UtcNow = new DateTimeOffset(2026, 06, 14, 3, 0, 0, TimeSpan.Zero) };

    // A published form assigned to PG, with all three rules on + one optional text field.
    private const string RulesSchema =
        "{\"fields\":[{\"id\":\"q1\",\"type\":\"text\",\"label_vi\":\"x\",\"label_en\":\"x\",\"required\":false}],"
        + "\"rules\":{\"gps_required\":true,\"photo_required\":true,\"require_check_in\":true}}";

    private static async Task<Guid> PublishedFormWithRulesAsync(AppDbContext db)
    {
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateFormCommand("RULES-1", "Báo cáo", "Report", null, null, "visit_report", RulesSchema), default);
        await new PublishFormCommandHandler(db, new InMemoryAuditLogger(), Clock, new TestCurrentUser { UserId = Guid.NewGuid() })
            .Handle(new PublishFormCommand(create.Value), default);
        await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(create.Value, "pg", null, null, null, null, null, null, null), default);
        return create.Value;
    }

    private static void SeedCheckInToday(AppDbContext db, Guid userId) =>
        db.AttendanceRecords.Add(AttendanceRecord.CheckIn(new AttendanceCheckInData(
            userId, Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, 10m, 106m, 10m, false,
            FaceVerificationResult.Success, 0.99m, "s", "p", false, null)));

    private static SubmitFormCommand Cmd(Guid formId, Guid pg, string key,
        string? attachments = null, decimal? lat = null, decimal? lng = null) =>
        new(formId, pg, UserRole.Pg, "{}", attachments, null, 10, key, lat, lng);

    [Fact]
    public async Task Submit_GpsRequired_MissingCoords_Rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedFormWithRulesAsync(db);
        var pg = Guid.NewGuid();

        var res = await new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(Cmd(formId, pg, "k1"), default);

        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
        res.Error.Message.Should().Contain("GPS");
    }

    [Fact]
    public async Task Submit_PhotoRequired_NoAttachment_Rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedFormWithRulesAsync(db);
        var pg = Guid.NewGuid();

        // GPS supplied, but no attachment → fails on photo rule.
        var res = await new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(Cmd(formId, pg, "k2", attachments: "{}", lat: 10.7m, lng: 106.7m), default);

        res.IsFailure.Should().BeTrue();
        res.Error.Message.Should().Contain("ảnh");
    }

    [Fact]
    public async Task Submit_RequireCheckIn_NotCheckedIn_Rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedFormWithRulesAsync(db);
        var pg = Guid.NewGuid();

        // GPS + photo supplied, but no attendance today → fails on check-in rule.
        var res = await new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(Cmd(formId, pg, "k3", attachments: "{\"q_img\":\"obj-key\"}", lat: 10.7m, lng: 106.7m), default);

        res.IsFailure.Should().BeTrue();
        res.Error.Message.Should().Contain("check-in");
    }

    [Fact]
    public async Task Submit_AllRulesSatisfied_Succeeds_AndPersistsGps()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedFormWithRulesAsync(db);
        var pg = Guid.NewGuid();
        SeedCheckInToday(db, pg);
        await db.SaveChangesAsync();

        var res = await new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(Cmd(formId, pg, "k4", attachments: "{\"q_img\":\"obj-key\"}", lat: 10.762m, lng: 106.66m), default);

        res.IsSuccess.Should().BeTrue();
        var saved = db.FormSubmissions.Single(s => s.Id == res.Value);
        saved.GpsLatitude.Should().Be(10.762m);
        saved.GpsLongitude.Should().Be(106.66m);
    }
}
