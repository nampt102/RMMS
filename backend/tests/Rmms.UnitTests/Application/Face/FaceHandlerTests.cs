using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.FaceRecognition;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Face;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Face;

public sealed class FaceHandlerTests
{
    private static PhotoUpload Photo() => new("f.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
    private static PhotoUpload[] ThreeAngles() => new[] { Photo(), Photo(), Photo() };

    // ----- Enroll -----

    [Fact]
    public async Task Enroll_RecordsTemplate_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var pg = UserFactory.CreateActivePg();
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var face = new FakeFaceClient();
        var result = await new EnrollFaceCommandHandler(db, face, audit, clock)
            .Handle(new EnrollFaceCommand(pg.Id, ThreeAngles()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Enrolled.Should().BeTrue();
        var saved = db.Users.Single(u => u.Id == pg.Id);
        saved.FaceTemplateExternalId.Should().Be(pg.Id.ToString());
        saved.FaceEnrolledAt.Should().NotBeNull();
        face.EnrollCalls.Should().Be(1);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.FaceEnrolled);
    }

    [Fact]
    public async Task Enroll_WhenEngineDown_ReturnsUpstreamUnavailable()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg();
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var result = await new EnrollFaceCommandHandler(db, new FakeFaceClient(throwOnCall: true), new InMemoryAuditLogger(), new TestClock())
            .Handle(new EnrollFaceCommand(pg.Id, ThreeAngles()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.UpstreamUnavailable);
        db.Users.Single(u => u.Id == pg.Id).FaceTemplateExternalId.Should().BeNull();
    }

    // ----- Admin remove -----

    [Fact]
    public async Task AdminRemove_ClearsEnrollment_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var audit = new InMemoryAuditLogger();
        var pg = UserFactory.CreateActivePg();
        pg.RecordFaceEnrollment(pg.Id.ToString(), clock.UtcNow);
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var face = new FakeFaceClient();
        var result = await new AdminRemoveFaceCommandHandler(db, face, audit)
            .Handle(new AdminRemoveFaceCommand(pg.Id, Guid.NewGuid(), ReEnroll: true), default);

        result.IsSuccess.Should().BeTrue();
        db.Users.Single(u => u.Id == pg.Id).FaceTemplateExternalId.Should().BeNull();
        face.LastDeletedSubject.Should().Be(pg.Id.ToString());
        audit.Calls.Should().Contain(c => c.Action == AuditAction.FaceRemoved);
    }

    // ----- FaceVerificationService (M05 port) -----

    private static FaceVerificationService Service(AppDbContext db, FakeFaceClient face) =>
        new(db, face, NullLogger<FaceVerificationService>.Instance);

    [Fact]
    public async Task Verify_NotEnrolled_PendingReview()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = UserFactory.CreateActivePg();
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var outcome = await Service(db, new FakeFaceClient()).VerifyAsync(pg.Id, Photo(), default);
        outcome.Result.Should().Be(FaceVerificationResult.PendingReview);
    }

    [Fact]
    public async Task Verify_EnrolledMatch_Success()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg();
        pg.RecordFaceEnrollment(pg.Id.ToString(), clock.UtcNow);
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var outcome = await Service(db, new FakeFaceClient(match: true)).VerifyAsync(pg.Id, Photo(), default);
        outcome.Result.Should().Be(FaceVerificationResult.Success);
    }

    [Fact]
    public async Task Verify_EnrolledNoMatch_Fail()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg();
        pg.RecordFaceEnrollment(pg.Id.ToString(), clock.UtcNow);
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var outcome = await Service(db, new FakeFaceClient(match: false)).VerifyAsync(pg.Id, Photo(), default);
        outcome.Result.Should().Be(FaceVerificationResult.Fail);
    }

    [Fact]
    public async Task Verify_EngineDown_PendingReview()
    {
        await using var db = TestDbContextFactory.Create();
        var clock = new TestClock();
        var pg = UserFactory.CreateActivePg();
        pg.RecordFaceEnrollment(pg.Id.ToString(), clock.UtcNow);
        db.Users.Add(pg);
        await db.SaveChangesAsync();

        var outcome = await Service(db, new FakeFaceClient(throwOnCall: true)).VerifyAsync(pg.Id, Photo(), default);
        outcome.Result.Should().Be(FaceVerificationResult.PendingReview);
    }
}
