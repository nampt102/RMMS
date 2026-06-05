using FluentAssertions;
using Rmms.Application.Organization.Areas;
using Rmms.Application.Organization.Categories;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class AreaCategoryHandlerTests
{
    // ----- Areas -----

    [Fact]
    public async Task CreateArea_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var audit = new InMemoryAuditLogger();

        var result = await new CreateAreaCommandHandler(db, audit)
            .Handle(new CreateAreaCommand("HCM", "Ho Chi Minh", null), default);

        result.IsSuccess.Should().BeTrue();
        db.Areas.Single().Code.Should().Be("HCM");
        audit.Calls.Should().Contain(c => c.Action == AuditAction.AreaCreated);
    }

    [Fact]
    public async Task CreateArea_DuplicateCode_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Areas.Add(Area.Create("HCM", "Existing", null));
        await db.SaveChangesAsync();

        var result = await new CreateAreaCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateAreaCommand("HCM", "Dup", null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CodeAlreadyExists);
    }

    [Fact]
    public async Task CreateArea_UnknownParent_ReturnsInvalidReference()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new CreateAreaCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateAreaCommand("HCM-Q1", "Q1", Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidReference);
    }

    [Fact]
    public async Task UpdateArea_SelfParent_ReturnsInvalidReference()
    {
        await using var db = TestDbContextFactory.Create();
        var area = Area.Create("HCM", "HCM", null);
        db.Areas.Add(area);
        await db.SaveChangesAsync();

        var result = await new UpdateAreaCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateAreaCommand(area.Id, "HCM", area.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidReference);
    }

    [Fact]
    public async Task DeleteArea_WithStores_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        var area = Area.Create("HCM", "HCM", null);
        db.Areas.Add(area);
        db.Stores.Add(Store.Create("ST-1", "S", null, 1m, 1m, area.Id));
        await db.SaveChangesAsync();

        var result = await new DeleteAreaCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new DeleteAreaCommand(area.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    [Fact]
    public async Task DeleteArea_NoStores_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var area = Area.Create("HCM", "HCM", null);
        db.Areas.Add(area);
        await db.SaveChangesAsync();

        var result = await new DeleteAreaCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new DeleteAreaCommand(area.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.Areas.Any().Should().BeFalse();
    }

    [Fact]
    public async Task GetAreas_ReturnsParentName()
    {
        await using var db = TestDbContextFactory.Create();
        var parent = Area.Create("HCM", "Ho Chi Minh", null);
        db.Areas.Add(parent);
        await db.SaveChangesAsync();
        db.Areas.Add(Area.Create("HCM-Q1", "Quan 1", parent.Id));
        await db.SaveChangesAsync();

        var result = await new GetAreasQueryHandler(db).Handle(new GetAreasQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(a => a.Code == "HCM-Q1" && a.ParentAreaName == "Ho Chi Minh");
    }

    // ----- Categories -----

    [Fact]
    public async Task CreateCategory_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var audit = new InMemoryAuditLogger();

        var result = await new CreateCategoryCommandHandler(db, audit)
            .Handle(new CreateCategoryCommand("BEV", "Beverages"), default);

        result.IsSuccess.Should().BeTrue();
        db.Categories.Single().Code.Should().Be("BEV");
        audit.Calls.Should().Contain(c => c.Action == AuditAction.CategoryCreated);
    }

    [Fact]
    public async Task CreateCategory_DuplicateCode_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Categories.Add(Category.Create("BEV", "Existing"));
        await db.SaveChangesAsync();

        var result = await new CreateCategoryCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateCategoryCommand("BEV", "Dup"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CodeAlreadyExists);
    }

    [Fact]
    public async Task UpdateCategory_NotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new UpdateCategoryCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateCategoryCommand(Guid.NewGuid(), "x"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
