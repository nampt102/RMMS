using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rmms.Domain.Enums;
using Rmms.Domain.Forms;

namespace Rmms.Infrastructure.Persistence.Configurations;

internal sealed class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> b)
    {
        b.ToTable("forms");
        b.HasKey(f => f.Id);

        b.Property(f => f.Code).IsRequired().HasMaxLength(50);
        b.Property(f => f.NameVi).IsRequired().HasMaxLength(255);
        b.Property(f => f.NameEn).IsRequired().HasMaxLength(255);
        b.Property(f => f.DescriptionVi).HasColumnType("text");
        b.Property(f => f.DescriptionEn).HasColumnType("text");
        b.Property(f => f.CurrentVersion).IsRequired();

        b.Property(f => f.FormType)
            .HasConversion(v => FormTypeToString(v), v => FormTypeFromString(v))
            .HasMaxLength(50)
            .IsRequired();

        b.Property(f => f.Status)
            .HasConversion(v => FormStatusToString(v), v => FormStatusFromString(v))
            .HasMaxLength(20)
            .IsRequired();

        b.HasIndex(f => f.Code)
            .IsUnique()
            .HasDatabaseName("ix_forms_code_unique")
            .HasFilter("deleted_at IS NULL");

        b.HasIndex(f => new { f.Status, f.DeletedAt })
            .HasDatabaseName("ix_forms_status_deleted_at")
            .HasFilter("deleted_at IS NULL");

        b.HasQueryFilter(f => f.DeletedAt == null);
    }

    private static string FormTypeToString(FormType v) => v switch
    {
        FormType.StockReport => "stock_report",
        FormType.MarketReport => "market_report",
        FormType.PhotoReport => "photo_report",
        FormType.PcChecklist => "pc_checklist",
        FormType.FreeReport => "free_report",
        FormType.Survey => "survey",
        FormType.KnowledgeTest => "knowledge_test",
        FormType.Training => "training",
        FormType.VisitReport => "visit_report",
        _ => throw new InvalidOperationException($"Unknown FormType value: {v}"),
    };

    private static FormType FormTypeFromString(string v) => v switch
    {
        "stock_report" => FormType.StockReport,
        "market_report" => FormType.MarketReport,
        "photo_report" => FormType.PhotoReport,
        "pc_checklist" => FormType.PcChecklist,
        "free_report" => FormType.FreeReport,
        "survey" => FormType.Survey,
        "knowledge_test" => FormType.KnowledgeTest,
        "training" => FormType.Training,
        "visit_report" => FormType.VisitReport,
        _ => throw new InvalidOperationException($"Unknown form type string: '{v}'"),
    };

    private static string FormStatusToString(FormStatus v) => v switch
    {
        FormStatus.Draft => "draft",
        FormStatus.Published => "published",
        FormStatus.Archived => "archived",
        _ => throw new InvalidOperationException($"Unknown FormStatus value: {v}"),
    };

    private static FormStatus FormStatusFromString(string v) => v switch
    {
        "draft" => FormStatus.Draft,
        "published" => FormStatus.Published,
        "archived" => FormStatus.Archived,
        _ => throw new InvalidOperationException($"Unknown form status string: '{v}'"),
    };
}
