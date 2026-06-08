using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Application.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Notifications;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(n => n.Id);

        b.Property(n => n.UserId).IsRequired();

        b.Property(n => n.Type)
            .HasConversion(v => v.ToSnakeCase(), v => FromSnake<NotificationType>(v))
            .HasMaxLength(50)
            .IsRequired();

        b.Property(n => n.Title).HasMaxLength(255).IsRequired();
        b.Property(n => n.Body).HasColumnType("text");
        b.Property(n => n.Data).HasColumnType("jsonb");

        b.Property(n => n.ChannelsSent)
            .HasColumnType("varchar(50)[]")
            .IsRequired();

        b.Property(n => n.IsRead).IsRequired();

        b.HasQueryFilter(n => n.DeletedAt == null);

        // Recipient's inbox: newest first + unread badge count.
        b.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("ix_notifications_user_read_created");
    }

    private static T FromSnake<T>(string v) where T : struct, Enum =>
        Enum.Parse<T>(v.Replace("_", string.Empty), ignoreCase: true);
}
