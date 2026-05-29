using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Devices;
using Rmms.Domain.Enums;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> b)
    {
        b.ToTable("user_devices");
        b.HasKey(d => d.Id);

        b.Property(d => d.UserId).IsRequired();
        b.Property(d => d.DeviceId).IsRequired().HasMaxLength(255);
        b.Property(d => d.DeviceName).IsRequired().HasMaxLength(255);
        b.Property(d => d.Os).HasMaxLength(20).IsRequired();
        b.Property(d => d.OsVersion).HasMaxLength(20).IsRequired();
        b.Property(d => d.AppVersion).HasMaxLength(20).IsRequired();
        b.Property(d => d.FcmToken).HasMaxLength(500);

        // HasConversion turns these lambdas into expression trees, which CANNOT contain
        // C# switch expressions or throw expressions. Static helpers (compile to MethodCall
        // nodes) are expression-tree-safe.
        b.Property(d => d.Status)
            .HasConversion(
                v => DeviceStatusToString(v),
                v => DeviceStatusFromString(v))
            .HasMaxLength(30)
            .IsRequired();

        // ----- Indexes -----

        // Lookup by user
        b.HasIndex(d => d.UserId).HasDatabaseName("ix_user_devices_user_id");

        // Lookup by raw device fingerprint
        b.HasIndex(d => new { d.UserId, d.DeviceId })
            .HasDatabaseName("ix_user_devices_user_device");

        // BR-105: at most ONE active device per user.
        // Postgres partial unique index — Sprint 02 enforces with explicit transaction
        // when approving a new device (mark old `Active` → `Replaced` then approve new).
        b.HasIndex(d => d.UserId)
            .IsUnique()
            .HasDatabaseName("ix_user_devices_one_active_per_user")
            .HasFilter("status = 'active'");
    }

    // ----- Static conversion helpers (referenced as method group inside HasConversion lambdas
    // so EF Core builds a MethodCall expression node instead of an inline switch expression). -----

    private static string DeviceStatusToString(DeviceStatus v)
    {
        return v switch
        {
            DeviceStatus.PendingApproval => "pending_approval",
            DeviceStatus.Active => "active",
            DeviceStatus.Rejected => "rejected",
            DeviceStatus.Replaced => "replaced",
            _ => throw new InvalidOperationException($"Unknown DeviceStatus value: {v}"),
        };
    }

    private static DeviceStatus DeviceStatusFromString(string v)
    {
        return v switch
        {
            "pending_approval" => DeviceStatus.PendingApproval,
            "active" => DeviceStatus.Active,
            "rejected" => DeviceStatus.Rejected,
            "replaced" => DeviceStatus.Replaced,
            _ => throw new InvalidOperationException($"Unknown device status string: '{v}'"),
        };
    }
}
