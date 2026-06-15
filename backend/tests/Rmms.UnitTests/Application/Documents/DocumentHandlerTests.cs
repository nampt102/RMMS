using FluentAssertions;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Documents;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Documents;

public sealed class DocumentHandlerTests
{
    private static PhotoUpload File(string name = "doc.pdf") =>
        new(name, "application/pdf", new byte[] { 1, 2, 3, 4 });

    private static async Task<Guid> UploadAsync(
        AppDbContext db, string folder = "public", string name = "Policy", Guid? by = null)
    {
        var res = await new UploadDocumentCommandHandler(db, new FakePhotoStorage(), new InMemoryAuditLogger())
            .Handle(new UploadDocumentCommand(name, null, folder, File(), by ?? Guid.NewGuid()), default);
        res.IsSuccess.Should().BeTrue();
        return res.Value;
    }

    [Fact]
    public void UploadValidator_RejectsEmptyName()
    {
        var result = new UploadDocumentCommandValidator()
            .Validate(new UploadDocumentCommand("", null, "public", File(), Guid.NewGuid()));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Upload_Public_Persists()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db);
        db.Documents.Single(d => d.Id == id).FolderType.Should().Be(DocumentFolderType.Public);
    }

    [Fact]
    public async Task Upload_InvalidFolder_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var res = await new UploadDocumentCommandHandler(db, new FakePhotoStorage(), new InMemoryAuditLogger())
            .Handle(new UploadDocumentCommand("X", null, "secret", File(), Guid.NewGuid()), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Assign_ToUser_Notifies_AndPersists()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db, folder: "private", name: "Payslip 06/2026");
        var pg = Guid.NewGuid();
        var notifier = new FakeNotificationService();

        var res = await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), notifier)
            .Handle(new AssignDocumentCommand(id, null, pg), default);

        res.IsSuccess.Should().BeTrue();
        db.DocumentAssignments.Should().ContainSingle(a => a.DocumentId == id && a.AssignedToUserId == pg);
        notifier.Sent.Should().ContainSingle(s => s.UserId == pg && s.Spec.Type == NotificationType.Payslip);
    }

    [Fact]
    public async Task Assign_NoTarget_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db);
        var res = await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
            .Handle(new AssignDocumentCommand(id, null, null), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task GetMyDocuments_ResolvesByRoleOrUser()
    {
        await using var db = TestDbContextFactory.Create();
        var roleDoc = await UploadAsync(db, name: "Handbook");
        var userDoc = await UploadAsync(db, folder: "private", name: "Payslip");
        var pg = Guid.NewGuid();
        await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
            .Handle(new AssignDocumentCommand(roleDoc, "pg", null), default);
        await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
            .Handle(new AssignDocumentCommand(userDoc, null, pg), default);

        var mine = await new GetMyDocumentsQueryHandler(db).Handle(new GetMyDocumentsQuery(pg, UserRole.Pg, null), default);
        mine.Value.Select(d => d.Id).Should().BeEquivalentTo(new[] { roleDoc, userDoc });

        // A leader (not the pg, no role match) sees neither.
        var leader = await new GetMyDocumentsQueryHandler(db).Handle(new GetMyDocumentsQuery(Guid.NewGuid(), UserRole.Leader, null), default);
        leader.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDocuments_SearchByName()
    {
        await using var db = TestDbContextFactory.Create();
        var a = await UploadAsync(db, name: "Company Handbook");
        await UploadAsync(db, name: "Safety Rules");
        var pg = Guid.NewGuid();
        foreach (var d in db.Documents.ToList())
        {
            await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
                .Handle(new AssignDocumentCommand(d.Id, "pg", null), default);
        }

        var res = await new GetMyDocumentsQueryHandler(db).Handle(new GetMyDocumentsQuery(pg, UserRole.Pg, "handbook"), default);
        res.Value.Select(d => d.Id).Should().ContainSingle().Which.Should().Be(a);
    }

    [Fact]
    public async Task Download_NotAssigned_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db, folder: "private");
        var res = await new GetDocumentDownloadUrlQueryHandler(db, new FakePhotoStorage(), new InMemoryAuditLogger())
            .Handle(new GetDocumentDownloadUrlQuery(id, Guid.NewGuid(), UserRole.Pg), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.PermissionDenied);
    }

    [Fact]
    public async Task Download_Assigned_ReturnsUrl_AndAuditsPrivate()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db, folder: "private", name: "Payslip");
        var pg = Guid.NewGuid();
        var audit = new InMemoryAuditLogger();
        await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
            .Handle(new AssignDocumentCommand(id, null, pg), default);

        var res = await new GetDocumentDownloadUrlQueryHandler(db, new FakePhotoStorage(), audit)
            .Handle(new GetDocumentDownloadUrlQuery(id, pg, UserRole.Pg), default);

        res.IsSuccess.Should().BeTrue();
        res.Value.Should().NotBeNullOrEmpty();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.DocumentDownloaded);
    }

    [Fact]
    public async Task Download_Admin_BypassesScope()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db, folder: "private");
        var res = await new GetDocumentDownloadUrlQueryHandler(db, new FakePhotoStorage(), new InMemoryAuditLogger())
            .Handle(new GetDocumentDownloadUrlQuery(id, Guid.NewGuid(), UserRole.Admin), default);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SoftDeletes_RemovesFromList()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await UploadAsync(db);
        var pg = Guid.NewGuid();
        await new AssignDocumentCommandHandler(db, new InMemoryAuditLogger(), new FakeNotificationService())
            .Handle(new AssignDocumentCommand(id, "pg", null), default);

        var del = await new DeleteDocumentCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new DeleteDocumentCommand(id), default);
        del.IsSuccess.Should().BeTrue();

        var mine = await new GetMyDocumentsQueryHandler(db).Handle(new GetMyDocumentsQuery(pg, UserRole.Pg, null), default);
        mine.Value.Should().BeEmpty();
    }
}
