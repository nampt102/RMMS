namespace Rmms.Domain.Enums;

/// <summary>
/// Store lifecycle status per <c>04-data-model.md</c> (stores.status) — M03.
/// Stored as lowercase string in DB.
/// </summary>
public enum StoreStatus
{
    /// <summary>Store is operational and can be assigned / checked in at.</summary>
    Active = 1,

    /// <summary>
    /// Store decommissioned. Per M03 edge case: ongoing schedules keep working until
    /// their end date, but no NEW assignments/schedules should target it.
    /// </summary>
    Inactive = 2,
}
