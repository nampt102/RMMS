using FluentAssertions;
using Rmms.Application.Forms;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Forms;

public sealed class FormHandlerTests
{
    private const string ValidSchema =
        "{\"fields\":[{\"id\":\"q1\",\"type\":\"text\",\"label_vi\":\"Tên\",\"label_en\":\"Name\",\"required\":true}],\"rules\":{}}";

    private static CreateFormCommand NewForm(string code = "F-SURVEY", string schema = ValidSchema) =>
        new(code, "Khảo sát", "Survey", null, null, "survey", schema);

    [Fact]
    public async Task CreateForm_Succeeds_WithDraftV1()
    {
        await using var db = TestDbContextFactory.Create();
        var audit = new InMemoryAuditLogger();

        var result = await new CreateFormCommandHandler(db, audit).Handle(NewForm(), default);

        result.IsSuccess.Should().BeTrue();
        var form = db.Forms.Single();
        form.Status.Should().Be(FormStatus.Draft);
        form.CurrentVersion.Should().Be(0);
        db.FormVersions.Single(v => v.FormId == form.Id).Version.Should().Be(1);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.FormCreated);
    }

    [Fact]
    public async Task CreateForm_DuplicateCode_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(NewForm(), default);

        var result = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(NewForm(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CodeAlreadyExists);
    }

    [Fact]
    public async Task CreateForm_InvalidSchema_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();

        // Unknown field type.
        var bad = NewForm(schema: "{\"fields\":[{\"id\":\"q1\",\"type\":\"nope\"}]}");
        var result = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(bad, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task CreateForm_InvalidFormType_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();

        var bad = new CreateFormCommand("F-X", "A", "A", null, null, "not_a_type", ValidSchema);
        var result = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(bad, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Publish_SetsCurrentVersionAndStatus()
    {
        await using var db = TestDbContextFactory.Create();
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(NewForm(), default);
        var formId = create.Value;
        var publisher = Guid.NewGuid();

        var publish = await new PublishFormCommandHandler(
            db, new InMemoryAuditLogger(), new TestClock(), new TestCurrentUser { UserId = publisher })
            .Handle(new PublishFormCommand(formId), default);

        publish.IsSuccess.Should().BeTrue();
        publish.Value.Should().Be(1);
        db.Forms.Single().CurrentVersion.Should().Be(1);
        db.Forms.Single().Status.Should().Be(FormStatus.Published);
        db.FormVersions.Single().IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task EditAfterPublish_CreatesNewDraftVersion_OldStaysImmutable()
    {
        await using var db = TestDbContextFactory.Create();
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(NewForm(), default);
        var formId = create.Value;
        await new PublishFormCommandHandler(db, new InMemoryAuditLogger(), new TestClock(), new TestCurrentUser { UserId = Guid.NewGuid() })
            .Handle(new PublishFormCommand(formId), default);

        // Edit the published form -> a new draft v2 is created; v1 stays published (BR-505).
        var edit = await new UpdateFormDraftCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateFormDraftCommand(formId, "Khảo sát v2", "Survey v2", null, null, ValidSchema), default);
        edit.IsSuccess.Should().BeTrue();

        var detail = await new GetFormQueryHandler(db).Handle(new GetFormQuery(formId), default);
        detail.Value.CurrentVersion.Should().Be(1);      // still v1 published
        detail.Value.EditableVersion.Should().Be(2);     // editing v2 draft
        detail.Value.HasDraft.Should().BeTrue();

        db.FormVersions.Count(v => v.FormId == formId).Should().Be(2);
        db.FormVersions.Single(v => v.Version == 1).IsPublished.Should().BeTrue();
        db.FormVersions.Single(v => v.Version == 2).IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Publish_NoDraft_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger()).Handle(NewForm(), default);
        var formId = create.Value;
        var publishHandler = new PublishFormCommandHandler(db, new InMemoryAuditLogger(), new TestClock(), new TestCurrentUser { UserId = Guid.NewGuid() });
        await publishHandler.Handle(new PublishFormCommand(formId), default);

        // Nothing new to publish.
        var again = await publishHandler.Handle(new PublishFormCommand(formId), default);

        again.IsFailure.Should().BeTrue();
        again.Error.Code.Should().Be(ErrorCodes.Conflict);
    }
}
