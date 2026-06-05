using FluentAssertions;
using Rmms.Application.Organization.Stores;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Infrastructure.Persistence;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class StoreHandlerTests
{
    private static Area SeedArea(AppDbContext db, string code = "HCM")
    {
        var a = Area.Create(code, "Ho Chi Minh", null);
        db.Areas.Add(a);
        return a;
    }

    [Fact]
    public async Task Create_Succeeds_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var area = SeedArea(db);
        await db.SaveChangesAsync();
        var audit = new InMemoryAuditLogger();

        var result = await new CreateStoreCommandHandler(db, audit)
            .Handle(new CreateStoreCommand("ST-1", "Store 1", "addr", 10.5m, 106.5m, area.Id), default);

        result.IsSuccess.Should().BeTrue();
        var store = db.Stores.Single();
        store.Code.Should().Be("ST-1");
        store.Status.Should().Be(StoreStatus.Active);
        store.AreaId.Should().Be(area.Id);
        audit.Calls.Should().Contain(c => c.Action == AuditAction.StoreCreated);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Stores.Add(Store.Create("ST-1", "Existing", null, 1m, 1m, null));
        await db.SaveChangesAsync();

        var result = await new CreateStoreCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateStoreCommand("ST-1", "Dup", null, 2m, 2m, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CodeAlreadyExists);
    }

    [Fact]
    public async Task Create_UnknownArea_ReturnsInvalidReference()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new CreateStoreCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateStoreCommand("ST-1", "S", null, 1m, 1m, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidReference);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new UpdateStoreCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateStoreCommand(Guid.NewGuid(), "x", null, 1m, 1m, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        await using var db = TestDbContextFactory.Create();
        var store = Store.Create("ST-1", "Old", null, 1m, 1m, null);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var result = await new UpdateStoreCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateStoreCommand(store.Id, "New name", "new addr", 20.1m, 105.2m, null), default);

        result.IsSuccess.Should().BeTrue();
        var updated = db.Stores.Single();
        updated.Name.Should().Be("New name");
        updated.Latitude.Should().Be(20.1m);
    }

    [Fact]
    public async Task ChangeStatus_Deactivates()
    {
        await using var db = TestDbContextFactory.Create();
        var store = Store.Create("ST-1", "S", null, 1m, 1m, null);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var result = await new ChangeStoreStatusCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new ChangeStoreStatusCommand(store.Id, "inactive"), default);

        result.IsSuccess.Should().BeTrue();
        db.Stores.Single().Status.Should().Be(StoreStatus.Inactive);
    }

    [Fact]
    public async Task Delete_RemovesStore_AndAudits()
    {
        await using var db = TestDbContextFactory.Create();
        var store = Store.Create("ST-1", "S", null, 1m, 1m, null);
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        var audit = new InMemoryAuditLogger();

        var result = await new DeleteStoreCommandHandler(db, audit)
            .Handle(new DeleteStoreCommand(store.Id), default);

        result.IsSuccess.Should().BeTrue();
        db.Stores.Any().Should().BeFalse();
        audit.Calls.Should().Contain(c => c.Action == AuditAction.StoreDeleted);
    }

    [Fact]
    public async Task GetStores_FiltersByStatus_AndPaginates()
    {
        await using var db = TestDbContextFactory.Create();
        db.Stores.Add(Store.Create("ST-1", "Active store", null, 1m, 1m, null));
        var inactive = Store.Create("ST-2", "Inactive store", null, 2m, 2m, null);
        inactive.Deactivate();
        db.Stores.Add(inactive);
        await db.SaveChangesAsync();

        var result = await new GetStoresQueryHandler(db)
            .Handle(new GetStoresQuery(1, 20, null, "active", null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().ContainSingle(s => s.Code == "ST-1");
        result.Value.Meta.Total.Should().Be(1);
    }

    [Theory]
    [InlineData(-91, 100)]
    [InlineData(91, 100)]
    [InlineData(10, 181)]
    public void CreateValidator_RejectsOutOfRangeCoords(double lat, double lon)
    {
        var r = new CreateStoreCommandValidator()
            .Validate(new CreateStoreCommand("ST-1", "S", null, (decimal)lat, (decimal)lon, null));
        r.IsValid.Should().BeFalse();
    }
}
