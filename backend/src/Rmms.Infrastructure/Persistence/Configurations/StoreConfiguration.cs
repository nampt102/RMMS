using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> b)
    {
        b.ToTable("stores");
        b.HasKey(s => s.Id);

        b.Property(s => s.Code).IsRequired().HasMaxLength(50);
        b.Property(s => s.Name).IsRequired().HasMaxLength(255);
        b.Property(s => s.Address); // text

        // numeric(10,7) per 04-data-model.md — ~1cm precision, range ±180.
        b.Property(s => s.Latitude).HasColumnType("numeric(10,7)").IsRequired();
        b.Property(s => s.Longitude).HasColumnType("numeric(10,7)").IsRequired();

        b.Property(s => s.AreaId);

        b.Property(s => s.Status)
            .HasConversion(
                v => StoreStatusToString(v),
                v => StoreStatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        // ----- Indexes -----
        b.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("ix_stores_code_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(s => s.AreaId).HasDatabaseName("ix_stores_area_id");
        b.HasIndex(s => new { s.Status, s.DeletedAt })
            .HasDatabaseName("ix_stores_status_deleted_at")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(s => s.DeletedAt == null);
    }

    // Static helpers keep the HasConversion lambdas expression-tree-safe (no inline switch).
    private static string StoreStatusToString(StoreStatus v)
    {
        return v switch
        {
            StoreStatus.Active => "active",
            StoreStatus.Inactive => "inactive",
            _ => throw new InvalidOperationException($"Unknown StoreStatus value: {v}"),
        };
    }

    private static StoreStatus StoreStatusFromString(string v)
    {
        return v switch
        {
            "active" => StoreStatus.Active,
            "inactive" => StoreStatus.Inactive,
            _ => throw new InvalidOperationException($"Unknown store status string: '{v}'"),
        };
    }
}
