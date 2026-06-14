using Rmms.Domain.Common;

namespace Rmms.Domain.News;

/// <summary>
/// A news / announcement (M14, <c>04-data-model.md</c> news). Bilingual title + content.
/// Lifecycle mirrors the Form Engine: created as a draft (<see cref="PublishedAt"/> null), then
/// published — publishing stamps <see cref="PublishedAt"/> and triggers notifications (CR-2).
/// <see cref="IsImportant"/> news requires the reader to confirm (acknowledge) it.
///
/// Named <c>NewsItem</c> (not <c>News</c>) to avoid the C# keyword-ish singular/plural clash and
/// keep the EF set <c>News</c> readable.
/// </summary>
public sealed class NewsItem : AuditableEntity, IAggregateRoot
{
    public string TitleVi { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string ContentVi { get; private set; } = string.Empty;
    public string ContentEn { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public bool IsImportant { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }

    public bool IsPublished => PublishedAt != null;

    private NewsItem() { } // EF Core

    public static NewsItem Create(
        string titleVi, string titleEn, string contentVi, string contentEn, string? category, bool isImportant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleVi);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleEn);

        return new NewsItem
        {
            TitleVi = titleVi.Trim(),
            TitleEn = titleEn.Trim(),
            ContentVi = contentVi?.Trim() ?? string.Empty,
            ContentEn = contentEn?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            IsImportant = isImportant,
        };
    }

    public void UpdateContent(
        string titleVi, string titleEn, string contentVi, string contentEn, string? category, bool isImportant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titleVi);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleEn);

        TitleVi = titleVi.Trim();
        TitleEn = titleEn.Trim();
        ContentVi = contentVi?.Trim() ?? string.Empty;
        ContentEn = contentEn?.Trim() ?? string.Empty;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        IsImportant = isImportant;
    }

    public void Publish(Guid publishedBy, DateTimeOffset now)
    {
        if (IsPublished) return;
        PublishedAt = now;
        PublishedBy = publishedBy;
    }
}
