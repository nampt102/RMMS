using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Users;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasKey(u => u.Id);

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        b.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email_unique");

        b.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        b.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(255);

        b.Property(u => u.Phone)
            .HasMaxLength(20);

        // Store enums as lowercase strings per 05-api-conventions.md "Enums".
        b.Property(u => u.Role)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<UserRole>(v, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();

        b.Property(u => u.Status)
            .HasConversion(
                v => ToDbString(v),
                v => FromDbString<UserStatus>(v))
            .HasMaxLength(30)
            .IsRequired();

        b.Property(u => u.PreferredLanguage)
            .HasMaxLength(5)
            .HasDefaultValue("vi")
            .IsRequired();

        b.Property(u => u.FaceTemplateExternalId)
            .HasMaxLength(255);

        // Phase 2 hooks — nullable, no values populated in Phase 1.
        b.Property(u => u.ExternalProvider).HasMaxLength(50);
        b.Property(u => u.ExternalId).HasMaxLength(255);
        b.Property(u => u.MfaEnabled).HasDefaultValue(false);
        b.Property(u => u.MfaSecretExternalId).HasMaxLength(255);

        // ----- Indexes -----
        b.HasIndex(u => new { u.Status, u.DeletedAt })
            .HasDatabaseName("ix_users_status_deleted_at")
            .HasFilter("deleted_at IS NULL"); // PostgreSQL partial index

        b.HasIndex(u => new { u.ExternalProvider, u.ExternalId })
            .HasDatabaseName("ix_users_external_identity")
            .HasFilter("external_provider IS NOT NULL");

        // ----- Domain events not persisted -----
        b.Ignore(u => u.DomainEvents);

        // Global query filter for soft-delete
        b.HasQueryFilter(u => u.DeletedAt == null);
    }

    /// <summary>
    /// Converts PascalCase enum to snake_case lowercase string for DB storage.
    /// Example: <c>PendingEmailVerify</c> → <c>pending_email_verify</c>.
    /// </summary>
    private static string ToDbString<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var raw = value.ToString();
        // Insert underscore before uppercase letters (except first).
        var chars = new System.Text.StringBuilder(raw.Length + 4);
        for (var i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]))
            {
                chars.Append('_');
            }
            chars.Append(char.ToLowerInvariant(raw[i]));
        }
        return chars.ToString();
    }

    private static TEnum FromDbString<TEnum>(string value) where TEnum : struct, Enum
    {
        // Strip underscores then case-insensitive parse: pending_email_verify -> PendingEmailVerify.
        var pascal = string.Concat(
            value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
        return Enum.Parse<TEnum>(pascal);
    }
}
