using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products");
        b.HasKey(p => p.Id);

        b.Property(p => p.Sku).IsRequired().HasMaxLength(100);
        b.Property(p => p.Name).IsRequired().HasMaxLength(255);
        b.Property(p => p.Brand).HasMaxLength(255);
        b.Property(p => p.CategoryId);

        // Flexible per-category attributes as Postgres jsonb (raw JSON string, like notifications.data).
        b.Property(p => p.Attributes).HasColumnType("jsonb");

        b.Property(p => p.Status)
            .HasConversion(
                v => ProductStatusToString(v),
                v => ProductStatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        // ----- Indexes -----
        b.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("ix_products_sku_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(p => p.CategoryId).HasDatabaseName("ix_products_category_id");
        b.HasIndex(p => new { p.Status, p.DeletedAt })
            .HasDatabaseName("ix_products_status_deleted_at")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(p => p.DeletedAt == null);
    }

    // Static helpers keep the HasConversion lambdas expression-tree-safe.
    private static string ProductStatusToString(ProductStatus v) => v switch
    {
        ProductStatus.Active => "active",
        ProductStatus.Inactive => "inactive",
        _ => throw new InvalidOperationException($"Unknown ProductStatus value: {v}"),
    };

    private static ProductStatus ProductStatusFromString(string v) => v switch
    {
        "active" => ProductStatus.Active,
        "inactive" => ProductStatus.Inactive,
        _ => throw new InvalidOperationException($"Unknown product status string: '{v}'"),
    };
}
