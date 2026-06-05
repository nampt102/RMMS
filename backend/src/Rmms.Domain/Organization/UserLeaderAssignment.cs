using Rmms.Domain.Common;

namespace Rmms.Domain.Organization;

/// <summary>
/// Assignment of a PG to a Leader per M03 + <c>04-data-model.md</c> (user_leader_assignments).
///
/// Constraint: a PG has AT MOST ONE active Leader at a time. "Active" = open-ended
/// (<see cref="EffectiveTo"/> is null). Enforced by a partial unique index on
/// <c>pg_user_id WHERE effective_to IS NULL</c>.
///
/// Edge case (M03): re-assigning a PG to a new Leader ends the old assignment
/// (sets <see cref="EffectiveTo"/>); pending requests stay with the old Leader.
/// </summary>
public sealed class UserLeaderAssignment : AuditableEntity, IAggregateRoot
{
    public Guid PgUserId { get; private set; }
    public Guid LeaderUserId { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    private UserLeaderAssignment() { }

    public static UserLeaderAssignment Create(Guid pgUserId, Guid leaderUserId, DateOnly effectiveFrom)
    {
        if (pgUserId == Guid.Empty) throw new ArgumentException("PG user id is required.", nameof(pgUserId));
        if (leaderUserId == Guid.Empty) throw new ArgumentException("Leader user id is required.", nameof(leaderUserId));
        if (pgUserId == leaderUserId) throw new InvalidOperationException("A PG cannot be assigned to themselves as Leader.");

        return new UserLeaderAssignment
        {
            PgUserId = pgUserId,
            LeaderUserId = leaderUserId,
            EffectiveFrom = effectiveFrom,
        };
    }

    /// <summary>Whether this assignment is active on the given date.</summary>
    public bool IsActiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);

    /// <summary>End this assignment (e.g. when the PG is re-assigned to a new Leader).</summary>
    public void End(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
        {
            throw new InvalidOperationException("effective_to cannot precede effective_from.");
        }
        EffectiveTo = effectiveTo;
    }
}
