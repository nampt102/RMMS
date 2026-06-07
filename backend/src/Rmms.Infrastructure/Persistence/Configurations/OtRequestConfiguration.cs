using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Application.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.LeaveOt;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class OtRequestConfiguration : IEntityTypeConfiguration<OtRequest>
{
    public void Configure(EntityTypeBuilder<OtRequest> b)
    {
        b.ToTable("ot_requests");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.OtDate).HasColumnType("date").IsRequired();
        b.Property(x => x.StartTime).HasColumnType("time").IsRequired();
        b.Property(x => x.EndTime).HasColumnType("time").IsRequired();
        b.Property(x => x.Reason).HasColumnType("text");
        b.Property(x => x.Status)
            .HasConversion(v => v.ToSnakeCase(), v => Enum.Parse<RequestStatus>(v, ignoreCase: true))
            .HasMaxLength(20).IsRequired();
        b.Property(x => x.ApprovalId);

        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => new { x.UserId, x.OtDate }).HasDatabaseName("ix_ot_requests_user_date");
        b.HasIndex(x => x.Status).HasDatabaseName("ix_ot_requests_status");
    }
}
