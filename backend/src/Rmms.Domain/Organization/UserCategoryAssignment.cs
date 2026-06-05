using Rmms.Domain.Common;

namespace Rmms.Domain.Organization;

/// <summary>
/// Assignment of a user to a product Category per M03 + <c>04-data-model.md</c>
/// (user_category_assignments). Used to scope Form Engine content (M10) by category.
/// Simple link (no effective dating in Phase 1); a user may have many categories.
/// </summary>
public sealed class UserCategoryAssignment : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }

    private UserCategoryAssignment() { }

    public static UserCategoryAssignment Create(Guid userId, Guid categoryId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (categoryId == Guid.Empty) throw new ArgumentException("Category id is required.", nameof(categoryId));

        return new UserCategoryAssignment
        {
            UserId = userId,
            CategoryId = categoryId,
        };
    }
}
