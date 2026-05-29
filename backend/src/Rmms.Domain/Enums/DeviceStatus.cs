namespace Rmms.Domain.Enums;

/// <summary>
/// Device status per <c>04-data-model.md</c> and BR-105 / BR-106 in <c>06-business-rules.md</c>.
/// Stored as lowercase string in DB.
/// </summary>
public enum DeviceStatus
{
    /// <summary>Device recorded but Leader/Admin has not yet approved — PG cannot login on it.</summary>
    PendingApproval = 1,

    /// <summary>Current active device — exactly one per PG per BR-105.</summary>
    Active = 2,

    /// <summary>Leader/Admin rejected the device change request.</summary>
    Rejected = 3,

    /// <summary>Old device after a device change was approved.</summary>
    Replaced = 4,
}
