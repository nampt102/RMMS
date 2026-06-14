using FluentAssertions;
using Rmms.Application.Organization.Products;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;
using Rmms.UnitTests.Common;
using Xunit;

namespace Rmms.UnitTests.Application.Organization;

public sealed class ProductHandlerTests
{
    [Fact]
    public async Task CreateProduct_Succeeds()
    {
        await using var db = TestDbContextFactory.Create();
        var audit = new InMemoryAuditLogger();

        var result = await new CreateProductCommandHandler(db, audit)
            .Handle(new CreateProductCommand("SKU-1", "Coca 330ml", "Coca-Cola", null, null), default);

        result.IsSuccess.Should().BeTrue();
        db.Products.Single().Sku.Should().Be("SKU-1");
        audit.Calls.Should().Contain(c => c.Action == AuditAction.ProductCreated);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ReturnsConflict()
    {
        await using var db = TestDbContextFactory.Create();
        db.Products.Add(Product.Create("SKU-1", "Existing", null, null, null));
        await db.SaveChangesAsync();

        var result = await new CreateProductCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateProductCommand("SKU-1", "Dup", null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CodeAlreadyExists);
    }

    [Fact]
    public async Task CreateProduct_UnknownCategory_ReturnsInvalidReference()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new CreateProductCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateProductCommand("SKU-2", "X", null, Guid.NewGuid(), null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidReference);
    }

    [Fact]
    public async Task CreateProduct_InvalidAttributesJson_ReturnsValidationFailed()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new CreateProductCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new CreateProductCommand("SKU-3", "X", null, null, "{not-json"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task ChangeStatus_Deactivate_Succeeds_AndListActiveOnlyExcludesIt()
    {
        await using var db = TestDbContextFactory.Create();
        var p = Product.Create("SKU-4", "Pepsi", "Pepsi", null, null);
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var change = await new ChangeProductStatusCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new ChangeProductStatusCommand(p.Id, "inactive"), default);
        change.IsSuccess.Should().BeTrue();

        var activeOnly = await new GetProductsQueryHandler(db)
            .Handle(new GetProductsQuery(ActiveOnly: true), default);
        activeOnly.IsSuccess.Should().BeTrue();
        activeOnly.Value.Data.Should().NotContain(x => x.Id == p.Id);

        var all = await new GetProductsQueryHandler(db)
            .Handle(new GetProductsQuery(ActiveOnly: false), default);
        all.Value.Data.Should().Contain(x => x.Id == p.Id && x.Status == "inactive");
    }

    [Fact]
    public async Task GetProducts_SearchAndCategoryName_Work()
    {
        await using var db = TestDbContextFactory.Create();
        var cat = Category.Create("BEV", "Beverages");
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        db.Products.Add(Product.Create("SKU-A", "Coca 330ml", "Coca-Cola", cat.Id, null));
        db.Products.Add(Product.Create("SKU-B", "Lays Classic", "Lays", null, null));
        await db.SaveChangesAsync();

        var result = await new GetProductsQueryHandler(db)
            .Handle(new GetProductsQuery(Search: "Coca"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().ContainSingle(x => x.Sku == "SKU-A" && x.CategoryName == "Beverages");
    }

    [Fact]
    public async Task UpdateProduct_NotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new UpdateProductCommandHandler(db, new InMemoryAuditLogger())
            .Handle(new UpdateProductCommand(Guid.NewGuid(), "x", null, null, null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
