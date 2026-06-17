using FluentAssertions;
using Rmms.Application.Admin.Users.GetUsers;
using Rmms.Domain.Users;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Admin;

public sealed class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task EmptyDb_ReturnsEmptyPage()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());

        var result = await sut.Handle(new GetUsersQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().BeEmpty();
        result.Value.Meta.Total.Should().Be(0);
        result.Value.Meta.Page.Should().Be(1);
    }

    [Fact]
    public async Task Pagination_RespectsPageAndPageSize()
    {
        await using var db = TestDbContextFactory.Create();
        for (var i = 0; i < 25; i++)
        {
            db.Users.Add(UserFactory.CreatePgPendingVerify($"u{i:D2}@example.com"));
        }
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());

        var page2 = await sut.Handle(new GetUsersQuery(Page: 2, PageSize: 10), default);

        page2.IsSuccess.Should().BeTrue();
        page2.Value.Data.Should().HaveCount(10);
        page2.Value.Meta.Total.Should().Be(25);
        page2.Value.Meta.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task RoleFilter_ReturnsOnlyMatchingRole()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreatePgPendingVerify("pg1@example.com"));
        db.Users.Add(UserFactory.CreatePgPendingVerify("pg2@example.com"));
        db.Users.Add(UserFactory.CreateAdmin("admin@example.com"));
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());
        var result = await sut.Handle(new GetUsersQuery(Role: "admin"), default);

        result.Value.Data.Should().ContainSingle()
            .Which.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyMatchingStatus()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreatePgPendingVerify("pending@example.com"));
        db.Users.Add(UserFactory.CreateActivePg("active1@example.com"));
        db.Users.Add(UserFactory.CreateActivePg("active2@example.com"));
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());
        var result = await sut.Handle(new GetUsersQuery(Status: "active"), default);

        result.Value.Data.Should().HaveCount(2);
        result.Value.Data.Should().OnlyContain(u => u.Status == "active");
    }

    [Fact]
    public async Task SearchByEmail_CaseInsensitiveContains()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(UserFactory.CreateActivePg("alice@example.com"));
        db.Users.Add(UserFactory.CreateActivePg("bob@example.com"));
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());
        var result = await sut.Handle(new GetUsersQuery(Search: "ALICE"), default);

        result.Value.Data.Should().ContainSingle()
            .Which.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task PageSize_CappedAtMaxLimit()
    {
        await using var db = TestDbContextFactory.Create();
        for (var i = 0; i < 5; i++)
        {
            db.Users.Add(UserFactory.CreatePgPendingVerify($"u{i}@example.com"));
        }
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());
        var result = await sut.Handle(new GetUsersQuery(PageSize: 9999), default);

        result.Value.Meta.PageSize.Should().Be(100); // capped
    }

    [Fact]
    public async Task ResultsOrderedByCreatedAtDescending()
    {
        await using var db = TestDbContextFactory.Create();
        var old = UserFactory.CreatePgPendingVerify("old@example.com");
        var fresh = UserFactory.CreatePgPendingVerify("fresh@example.com");
        db.Users.Add(old);
        await db.SaveChangesAsync();
        await Task.Delay(10); // ensure different CreatedAt
        db.Users.Add(fresh);
        await db.SaveChangesAsync();

        var sut = new GetUsersQueryHandler(db, new TestCurrentUser());
        var result = await sut.Handle(new GetUsersQuery(), default);

        result.Value.Data[0].Email.Should().Be("fresh@example.com");
    }

    [Fact]
    public async Task SuperAdmin_HiddenFromNonSuperCaller_ButVisibleToSuperCaller()
    {
        await using var db = TestDbContextFactory.Create();
        var regular = UserFactory.CreateActivePg("pg@example.com");
        var superA = User.CreateSuperAdmin("root@example.com", "plain:Pwd12345", "Root");
        db.Users.AddRange(regular, superA);
        await db.SaveChangesAsync();

        // A normal admin/user must not see the super-admin row.
        var asRegular = new GetUsersQueryHandler(db, new TestCurrentUser { UserId = regular.Id });
        var hidden = await asRegular.Handle(new GetUsersQuery(PageSize: 100), default);
        hidden.Value.Data.Should().Contain(u => u.Id == regular.Id);
        hidden.Value.Data.Should().NotContain(u => u.Id == superA.Id);

        // A super-admin caller sees the super-admin row.
        var asSuper = new GetUsersQueryHandler(db, new TestCurrentUser { UserId = superA.Id });
        var seen = await asSuper.Handle(new GetUsersQuery(PageSize: 100), default);
        seen.Value.Data.Should().Contain(u => u.Id == superA.Id);
    }
}
