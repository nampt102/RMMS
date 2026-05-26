namespace Rmms.Shared.Pagination;

/// <summary>
/// Standard list response per <c>05-api-conventions.md</c> "Pagination" section.
/// </summary>
public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Data,
    PaginationMeta Meta);

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    long Total,
    int TotalPages)
{
    public static PaginationMeta Build(int page, int pageSize, long total) =>
        new(page, pageSize, total, (int)Math.Ceiling(total / (double)Math.Max(pageSize, 1)));
}

public sealed record PaginationQuery(int Page = 1, int PageSize = 20, string? Sort = null)
{
    public const int MaxPageSize = 100;

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 20,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };
}
