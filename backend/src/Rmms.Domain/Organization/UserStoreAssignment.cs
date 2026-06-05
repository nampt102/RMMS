using Rmms.Domain.Common;

namespace Rmms.Domain.Organization;

/// <summary>
/// Assignment of a user (PG or Leader) to a Store per M03 + <c>04-data-model.md</c>
/// (user_store_assignments). A user may have MULTIPLE active store assignments (1:N).
/// Active = open-ended (<see cref="EffectiveTo"/> null) or end date in the future.
/// </summary>
public sealed class UserStoreAssignment : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid StoreId { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    private UserStoreAssignment() { }

    public static UserStoreAssignment Create(Guid userId, Guid storeId, DateOnly effectiveFrom)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (storeId == Guid.Empty) throw new ArgumentException("Store id is required.", nameof(storeId));

        return new UserStoreAssignment
        {
            UserId = userId,
            StoreId = storeId,
            EffectiveFrom = effectiveFrom,
        };
    }

    public bool IsActiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);

    public void End(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
        {
            throw new InvalidOperationException("effective_to cannot precede effective_from.");
        }
        EffectiveTo = effectiveTo;
    }
}
