using Rmms.Domain.News;

namespace Rmms.Application.News;

/// <summary>News projected for the reader (M14), including this user's read/confirm state.</summary>
public sealed record NewsDto(
    Guid Id,
    string TitleVi,
    string TitleEn,
    string ContentVi,
    string ContentEn,
    string? Category,
    bool IsImportant,
    DateTimeOffset? PublishedAt,
    bool IsRead,
    bool IsConfirmed);

/// <summary>News projected for the admin list/editor (M14) — includes content for editing,
/// no per-user read state.</summary>
public sealed record AdminNewsDto(
    Guid Id,
    string TitleVi,
    string TitleEn,
    string ContentVi,
    string ContentEn,
    string? Category,
    bool IsImportant,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

internal static class NewsMapper
{
    public static NewsDto ToDto(NewsItem n, NewsRead? read) => new(
        n.Id, n.TitleVi, n.TitleEn, n.ContentVi, n.ContentEn, n.Category, n.IsImportant,
        n.PublishedAt, read is not null, read?.IsConfirmed ?? false);

    public static AdminNewsDto ToAdminDto(NewsItem n) => new(
        n.Id, n.TitleVi, n.TitleEn, n.ContentVi, n.ContentEn, n.Category, n.IsImportant, n.IsPublished, n.PublishedAt, n.CreatedAt);
}
