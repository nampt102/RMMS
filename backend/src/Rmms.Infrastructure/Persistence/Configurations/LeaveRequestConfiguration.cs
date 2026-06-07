using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Application.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("leave_requests");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.LeaveType)
            .HasConversion(v => v.ToSnakeCase(), v => Enum.Parse<LeaveType>(v.Replace("_", string.Empty), ignoreCase: true))
            .HasMaxLength(20).IsRequired();
        b.Property(x => x.StartDate).HasColumnType("date").IsRequired();
        b.Property(x => x.EndDate).HasColumnType("date").IsRequired();
        b.Property(x => x.StartTime).HasColumnType("time");
        b.Property(x => x.EndTime).HasColumnType("time");
        b.Property(x => x.Reason).HasColumnType("text");
        b.Property(x => x.Status)
            .HasConversion(v => v.ToSnakeCase(), v => Enum.Parse<RequestStatus>(v, ignoreCase: true))
            .HasMaxLength(20).IsRequired();
        b.Property(x => x.ApprovalId);
        b.Property(x => x.LinkedAttendanceId);

        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => new { x.UserId, x.StartDate }).HasDatabaseName("ix_leave_requests_user_start");
        b.HasIndex(x => x.Status).HasDatabaseName("ix_leave_requests_status");
    }
}
