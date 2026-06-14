using Rmms.Domain.Common;

namespace Rmms.Domain.News;

/// <summary>
/// A user's read (and, for important news, confirmation) state for one <see cref="NewsItem"/>
/// (M14, <c>04-data-model.md</c> news_reads). One row per (news, user); the unique index
/// enforces it. <see cref="ConfirmedAt"/> is set only when the user acknowledges important news.
/// </summary>
public sealed class NewsRead : AuditableEntity, IAggregateRoot
{
    public Guid NewsId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset ReadAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    private NewsRead() { } // EF Core

    public static NewsRead Create(Guid newsId, Guid userId, DateTimeOffset now)
    {
        if (newsId == Guid.Empty) throw new ArgumentException("News id is required.", nameof(newsId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));

        return new NewsRead { NewsId = newsId, UserId = userId, ReadAt = now, CreatedAt = now };
    }

    public bool IsConfirmed => ConfirmedAt != null;

    public void Confirm(DateTimeOffset now)
    {
        ConfirmedAt ??= now;
    }
}
