using FluentAssertions;
using Rmms.Application.News;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.News;

public sealed class NewsHandlerTests
{
    private static readonly TestClock Clock = new() { UtcNow = new DateTimeOffset(2026, 06, 15, 3, 0, 0, TimeSpan.Zero) };

    private static async Task<Guid> CreateAsync(AppDbContext db, bool important = false, string titleVi = "Thông báo")
    {
        var res = await new CreateNewsCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateNewsCommand(titleVi, "Notice", "Nội dung", "Body", "general", important), default);
        res.IsSuccess.Should().BeTrue();
        return res.Value;
    }

    private static async Task AssignAsync(AppDbContext db, Guid id, string? role, Guid? userId)
    {
        var res = await new AssignNewsCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new AssignNewsCommand(id, role, userId), default);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Draft_NotPublished()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await CreateAsync(db);
        db.News.Single(n => n.Id == id).IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Publish_NotifiesRecipients_AndMarksPublished()
    {
        await using var db = TestDbContextFactory.Create();
        var pgUser = UserFactory.CreateActivePg("pg1@example.com");
        db.Users.Add(pgUser);
        var directUser = Guid.NewGuid();
        await db.SaveChangesAsync();

        var id = await CreateAsync(db, important: true);
        await AssignAsync(db, id, "pg", null);
        await AssignAsync(db, id, null, directUser);

        var notifier = new FakeNotificationService();
        var res = await new PublishNewsCommandHandler(db, new InMemoryAuditLogger(), Clock, notifier)
            .Handle(new PublishNewsCommand(id, Guid.NewGuid()), default);

        res.IsSuccess.Should().BeTrue();
        db.News.Single(n => n.Id == id).IsPublished.Should().BeTrue();
        notifier.Sent.Select(s => s.UserId).Should().Contain(new[] { pgUser.Id, directUser });
        notifier.Sent.Should().OnlyContain(s => s.Spec.Type == NotificationType.News);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_Conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await CreateAsync(db);
        var handler = new PublishNewsCommandHandler(db, new InMemoryAuditLogger(), Clock, new FakeNotificationService());
        await handler.Handle(new PublishNewsCommand(id, Guid.NewGuid()), default);

        var second = await handler.Handle(new PublishNewsCommand(id, Guid.NewGuid()), default);
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Assign_NoTarget_ValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await CreateAsync(db);
        var res = await new AssignNewsCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new AssignNewsCommand(id, null, null), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task GetMyNews_OnlyPublishedAssigned_WithReadState()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Guid.NewGuid();
        var published = await CreateAsync(db, titleVi: "Đã phát hành");
        var draft = await CreateAsync(db, titleVi: "Nháp");
        await AssignAsync(db, published, "pg", null);
        await AssignAsync(db, draft, "pg", null);
        await new PublishNewsCommandHandler(db, new InMemoryAuditLogger(), Clock, new FakeNotificationService())
            .Handle(new PublishNewsCommand(published, Guid.NewGuid()), default);

        var before = await new GetMyNewsQueryHandler(db).Handle(new GetMyNewsQuery(pg, UserRole.Pg), default);
        before.Value.Should().ContainSingle(n => n.Id == published);
        before.Value.Single().IsRead.Should().BeFalse();

        await new MarkNewsReadCommandHandler(db, Clock).Handle(new MarkNewsReadCommand(published, pg), default);
        var after = await new GetMyNewsQueryHandler(db).Handle(new GetMyNewsQuery(pg, UserRole.Pg), default);
        after.Value.Single().IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkRead_IsIdempotent()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Guid.NewGuid();
        var id = await CreateAsync(db);
        var handler = new MarkNewsReadCommandHandler(db, Clock);
        await handler.Handle(new MarkNewsReadCommand(id, pg), default);
        await handler.Handle(new MarkNewsReadCommand(id, pg), default);
        db.NewsReads.Count(r => r.NewsId == id && r.UserId == pg).Should().Be(1);
    }

    [Fact]
    public async Task Confirm_Important_SetsConfirmed()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Guid.NewGuid();
        var id = await CreateAsync(db, important: true);

        var res = await new ConfirmNewsCommandHandler(db, Clock).Handle(new ConfirmNewsCommand(id, pg), default);
        res.IsSuccess.Should().BeTrue();
        db.NewsReads.Single(r => r.NewsId == id && r.UserId == pg).IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Confirm_NonImportant_ValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();
        var id = await CreateAsync(db, important: false);
        var res = await new ConfirmNewsCommandHandler(db, Clock).Handle(new ConfirmNewsCommand(id, Guid.NewGuid()), default);
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Delete_SoftDeletes_RemovesFromMyNews()
    {
        await using var db = TestDbContextFactory.Create();
        var pg = Guid.NewGuid();
        var id = await CreateAsync(db);
        await AssignAsync(db, id, "pg", null);
        await new PublishNewsCommandHandler(db, new InMemoryAuditLogger(), Clock, new FakeNotificationService())
            .Handle(new PublishNewsCommand(id, Guid.NewGuid()), default);

        await new DeleteNewsCommandHandler(db, new InMemoryAuditLogger()).Handle(new DeleteNewsCommand(id), default);

        var mine = await new GetMyNewsQueryHandler(db).Handle(new GetMyNewsQuery(pg, UserRole.Pg), default);
        mine.Value.Should().BeEmpty();
    }
}
