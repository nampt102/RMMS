using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Scheduling;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> b)
    {
        b.ToTable("work_schedules");
        b.HasKey(s => s.Id);

        b.Property(s => s.UserId).IsRequired();
        b.Property(s => s.ScheduleDate).HasColumnType("date").IsRequired();

        b.Property(s => s.Status)
            .HasConversion(v => StatusToString(v), v => StatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        b.Property(s => s.Version).IsRequired();
        b.Property(s => s.PreviousVersionId);
        b.Property(s => s.SubmittedAt);
        b.Property(s => s.ApprovedAt);
        b.Property(s => s.ApprovedBy);
        b.Property(s => s.RejectReason);

        // Aggregate: shifts are an OWNED collection of the WorkSchedule root. Owned-collection
        // semantics give clean wholesale replacement on edit (delete-all + insert) across every
        // provider, and shifts are always loaded with their schedule (no explicit Include needed).
        b.OwnsMany(s => s.Shifts, sb =>
        {
            sb.ToTable("work_schedule_shifts");
            sb.HasKey(sh => sh.Id);
            sb.WithOwner().HasForeignKey(sh => sh.WorkScheduleId);

            sb.Property(sh => sh.StoreId).IsRequired();
            sb.Property(sh => sh.StartTime).HasColumnType("time").IsRequired();
            sb.Property(sh => sh.EndTime).HasColumnType("time").IsRequired();
            sb.Property(sh => sh.Ordering).IsRequired();
            sb.Property(sh => sh.CreatedAt).IsRequired();

            sb.HasIndex(sh => sh.WorkScheduleId).HasDatabaseName("ix_work_schedule_shifts_schedule_id");
            sb.HasIndex(sh => sh.StoreId).HasDatabaseName("ix_work_schedule_shifts_store_id");
        });
        b.Metadata.FindNavigation(nameof(WorkSchedule.Shifts))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Range lookups by user + day (GetMySchedule). The "at most one approved per
        // user/day" invariant (BR-308 / AC-15) is enforced in the approval handler
        // (supersede-then-approve) — NOT a partial-unique index, which would trip during
        // the edit-approval transition when both old and new rows are momentarily approved.
        b.HasIndex(s => new { s.UserId, s.ScheduleDate }).HasDatabaseName("ix_work_schedules_user_date");

        b.HasIndex(s => s.Status).HasDatabaseName("ix_work_schedules_status");

        b.HasQueryFilter(s => s.DeletedAt == null);
    }

    private static string StatusToString(WorkScheduleStatus v) => v switch
    {
        WorkScheduleStatus.Pending => "pending",
        WorkScheduleStatus.Approved => "approved",
        WorkScheduleStatus.Rejected => "rejected",
        WorkScheduleStatus.EditPending => "edit_pending",
        WorkScheduleStatus.Superseded => "superseded",
        _ => throw new InvalidOperationException($"Unknown WorkScheduleStatus value: {v}"),
    };

    private static WorkScheduleStatus StatusFromString(string v) => v switch
    {
        "pending" => WorkScheduleStatus.Pending,
        "approved" => WorkScheduleStatus.Approved,
        "rejected" => WorkScheduleStatus.Rejected,
        "edit_pending" => WorkScheduleStatus.EditPending,
        "superseded" => WorkScheduleStatus.Superseded,
        _ => throw new InvalidOperationException($"Unknown work schedule status string: '{v}'"),
    };
}
