using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.News;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class NewsItemConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> b)
    {
        b.ToTable("news");
        b.HasKey(n => n.Id);

        b.Property(n => n.TitleVi).HasMaxLength(255).IsRequired();
        b.Property(n => n.TitleEn).HasMaxLength(255).IsRequired();
        b.Property(n => n.ContentVi).IsRequired();
        b.Property(n => n.ContentEn).IsRequired();
        b.Property(n => n.Category).HasMaxLength(50);
        b.Property(n => n.IsImportant).IsRequired();
        b.Property(n => n.PublishedAt);
        b.Property(n => n.PublishedBy);

        b.HasIndex(n => n.PublishedAt).HasDatabaseName("ix_news_published_at");

        b.HasQueryFilter(n => n.DeletedAt == null);
    }
}

internal sealed class NewsAssignmentConfiguration : IEntityTypeConfiguration<NewsAssignment>
{
    public void Configure(EntityTypeBuilder<NewsAssignment> b)
    {
        b.ToTable("news_assignments");
        b.HasKey(a => a.Id);

        b.Property(a => a.NewsId).IsRequired();
        b.Property(a => a.AssignedToRole).HasMaxLength(20);
        b.Property(a => a.AssignedToUserId);

        b.HasIndex(a => a.NewsId).HasDatabaseName("ix_news_assignments_news_id");
        b.HasIndex(a => a.AssignedToUserId).HasDatabaseName("ix_news_assignments_user_id");

        b.HasQueryFilter(a => a.DeletedAt == null);
    }
}

internal sealed class NewsReadConfiguration : IEntityTypeConfiguration<NewsRead>
{
    public void Configure(EntityTypeBuilder<NewsRead> b)
    {
        b.ToTable("news_reads");
        b.HasKey(r => r.Id);

        b.Property(r => r.NewsId).IsRequired();
        b.Property(r => r.UserId).IsRequired();
        b.Property(r => r.ReadAt).IsRequired();
        b.Property(r => r.ConfirmedAt);

        // One read row per (news, user).
        b.HasIndex(r => new { r.NewsId, r.UserId })
            .IsUnique()
            .HasDatabaseName("ix_news_reads_news_user_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(r => r.DeletedAt == null);
    }
}
