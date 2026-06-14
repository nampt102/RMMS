using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Forms;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> b)
    {
        b.ToTable("form_submissions");
        b.HasKey(s => s.Id);

        b.Property(s => s.FormId).IsRequired();
        b.Property(s => s.FormVersionId).IsRequired();
        b.Property(s => s.UserId).IsRequired();
        b.Property(s => s.Answers).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.Attachments).HasColumnType("jsonb");
        b.Property(s => s.Score).HasColumnType("numeric(5,2)");
        b.Property(s => s.TimeSpentSeconds).IsRequired();
        b.Property(s => s.ClientIdempotencyKey).HasMaxLength(100).IsRequired();

        b.Property(s => s.Status)
            .HasConversion(v => StatusToString(v), v => StatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        b.HasIndex(s => new { s.FormId, s.UserId }).HasDatabaseName("ix_form_submissions_form_user");

        // Offline-retry dedup: one submission per (user, client key).
        b.HasIndex(s => new { s.UserId, s.ClientIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_form_submissions_user_idem_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(s => s.DeletedAt == null);
    }

    private static string StatusToString(FormSubmissionStatus v) => v switch
    {
        FormSubmissionStatus.Submitted => "submitted",
        FormSubmissionStatus.DraftOffline => "draft_offline",
        FormSubmissionStatus.Edited => "edited",
        _ => throw new InvalidOperationException($"Unknown FormSubmissionStatus value: {v}"),
    };

    private static FormSubmissionStatus StatusFromString(string v) => v switch
    {
        "submitted" => FormSubmissionStatus.Submitted,
        "draft_offline" => FormSubmissionStatus.DraftOffline,
        "edited" => FormSubmissionStatus.Edited,
        _ => throw new InvalidOperationException($"Unknown form submission status string: '{v}'"),
    };
}
