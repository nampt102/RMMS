using Rmms.Domain.Common;

namespace Rmms.Domain.News;

/// <summary>
/// One targeting rule for a <see cref="NewsItem"/> (M14, <c>04-data-model.md</c> news_assignments).
/// Multiple rows = OR logic; a null field means "not scoped by that dimension".
/// </summary>
public sealed class NewsAssignment : AuditableEntity, IAggregateRoot
{
    public Guid NewsId { get; private set; }
    public string? AssignedToRole { get; private set; }
    public Guid? AssignedToUserId { get; private set; }

    private NewsAssignment() { } // EF Core

    public static NewsAssignment Create(Guid newsId, string? role, Guid? userId)
    {
        if (newsId == Guid.Empty) throw new ArgumentException("News id is required.", nameof(newsId));
        if (string.IsNullOrWhiteSpace(role) && userId is null)
        {
            throw new ArgumentException("An assignment needs a role or a user.", nameof(userId));
        }

        return new NewsAssignment
        {
            NewsId = newsId,
            AssignedToRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToLowerInvariant(),
            AssignedToUserId = userId,
        };
    }
}
