using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.VisitPlan;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class VisitPlanConfiguration : IEntityTypeConfiguration<VisitPlan>
{
    public void Configure(EntityTypeBuilder<VisitPlan> b)
    {
        b.ToTable("visit_plans");
        b.HasKey(p => p.Id);

        b.Property(p => p.LeaderUserId).IsRequired();
        b.Property(p => p.VisitDate).HasColumnType("date").IsRequired();
        b.Property(p => p.Notes);
        b.Property(p => p.ApprovalId);

        b.Property(p => p.Status)
            .HasConversion(v => StatusToString(v), v => StatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        // Aggregate: items are an OWNED collection of the VisitPlan root — always loaded with the
        // plan and wholesale-replaced on a pending edit.
        b.OwnsMany(p => p.Items, ib =>
        {
            ib.ToTable("visit_plan_items");
            ib.HasKey(i => i.Id);
            ib.WithOwner().HasForeignKey(i => i.VisitPlanId);

            ib.Property(i => i.StoreId).IsRequired();
            ib.Property(i => i.FormId).IsRequired();
            ib.Property(i => i.Ordering).IsRequired();
            ib.Property(i => i.ExecutedAt);
            ib.Property(i => i.FormSubmissionId);
            ib.Property(i => i.CreatedAt).IsRequired();

            ib.HasIndex(i => i.VisitPlanId).HasDatabaseName("ix_visit_plan_items_plan_id");
            ib.HasIndex(i => i.StoreId).HasDatabaseName("ix_visit_plan_items_store_id");
        });
        b.Metadata.FindNavigation(nameof(VisitPlan.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(p => new { p.LeaderUserId, p.VisitDate }).HasDatabaseName("ix_visit_plans_leader_date");
        b.HasIndex(p => p.Status).HasDatabaseName("ix_visit_plans_status");

        b.HasQueryFilter(p => p.DeletedAt == null);
    }

    private static string StatusToString(VisitPlanStatus v) => v switch
    {
        VisitPlanStatus.Pending => "pending",
        VisitPlanStatus.Approved => "approved",
        VisitPlanStatus.Rejected => "rejected",
        VisitPlanStatus.Executed => "executed",
        _ => throw new InvalidOperationException($"Unknown VisitPlanStatus value: {v}"),
    };

    private static VisitPlanStatus StatusFromString(string v) => v switch
    {
        "pending" => VisitPlanStatus.Pending,
        "approved" => VisitPlanStatus.Approved,
        "rejected" => VisitPlanStatus.Rejected,
        "executed" => VisitPlanStatus.Executed,
        _ => throw new InvalidOperationException($"Unknown visit plan status string: '{v}'"),
    };
}
