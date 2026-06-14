using FluentAssertions;
using Rmms.Application.Forms;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Forms;

public sealed class FormFillHandlerTests
{
    private const string Schema =
        "{\"fields\":[{\"id\":\"q1\",\"type\":\"text\",\"label_vi\":\"Tên\",\"label_en\":\"Name\",\"required\":true}],\"rules\":{}}";

    private static readonly TestClock Clock = new() { UtcNow = new DateTimeOffset(2026, 06, 14, 3, 0, 0, TimeSpan.Zero) };

    // Create + publish a form, returns its id.
    private static async Task<Guid> PublishedForm(AppDbContext db, string code = "F-1")
    {
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateFormCommand(code, "Khảo sát", "Survey", null, null, "survey", Schema), default);
        await new PublishFormCommandHandler(db, new InMemoryAuditLogger(), Clock, new TestCurrentUser { UserId = Guid.NewGuid() })
            .Handle(new PublishFormCommand(create.Value), default);
        return create.Value;
    }

    [Fact]
    public async Task AssignForm_NoTarget_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedForm(db);

        var result = await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(formId, null, null, null, null, null, null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task GetMyForms_ResolvesByRole_PublishedOnly()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedForm(db);
        await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(formId, "pg", null, null, null, null, null, null, null), default);

        var pg = await new GetMyFormsQueryHandler(db, Clock).Handle(new GetMyFormsQuery(Guid.NewGuid(), UserRole.Pg), default);
        pg.Value.Should().ContainSingle(f => f.FormId == formId);

        var leader = await new GetMyFormsQueryHandler(db, Clock).Handle(new GetMyFormsQuery(Guid.NewGuid(), UserRole.Leader), default);
        leader.Value.Should().BeEmpty(); // assigned to pg only
    }

    [Fact]
    public async Task GetMyForms_DraftForm_NotReturned()
    {
        await using var db = TestDbContextFactory.Create();
        // Create but DON'T publish.
        var create = await new CreateFormCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateFormCommand("F-DRAFT", "A", "A", null, null, "survey", Schema), default);
        await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(create.Value, "pg", null, null, null, null, null, null, null), default);

        var pg = await new GetMyFormsQueryHandler(db, Clock).Handle(new GetMyFormsQuery(Guid.NewGuid(), UserRole.Pg), default);

        pg.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFormForFill_NotAssigned_ReturnsFormNotAssigned()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedForm(db);

        var result = await new GetFormForFillQueryHandler(db, Clock)
            .Handle(new GetFormForFillQuery(formId, Guid.NewGuid(), UserRole.Pg), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.FormNotAssigned);
    }

    [Fact]
    public async Task SubmitForm_MissingRequired_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedForm(db);
        await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(formId, "pg", null, null, null, null, null, null, null), default);
        var pg = Guid.NewGuid();

        var result = await new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new SubmitFormCommand(formId, pg, UserRole.Pg, "{}", null, null, 10, "key-1"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task SubmitForm_Succeeds_AndIdempotentRetryReturnsSame()
    {
        await using var db = TestDbContextFactory.Create();
        var formId = await PublishedForm(db);
        await new AssignFormCommandHandler(db, new InMemoryAuditLogger(), Clock)
            .Handle(new AssignFormCommand(formId, "pg", null, null, null, null, null, null, null), default);
        var pg = Guid.NewGuid();
        var handler = new SubmitFormCommandHandler(db, new InMemoryAuditLogger(), Clock);
        var answers = "{\"q1\":\"Nguyen Van A\"}";

        var first = await handler.Handle(new SubmitFormCommand(formId, pg, UserRole.Pg, answers, null, null, 30, "key-9"), default);
        first.IsSuccess.Should().BeTrue();

        // Same client key -> dedup, returns the same submission, no second row.
        var retry = await handler.Handle(new SubmitFormCommand(formId, pg, UserRole.Pg, answers, null, null, 30, "key-9"), default);
        retry.IsSuccess.Should().BeTrue();
        retry.Value.Should().Be(first.Value);
        db.FormSubmissions.Count().Should().Be(1);
    }
}
