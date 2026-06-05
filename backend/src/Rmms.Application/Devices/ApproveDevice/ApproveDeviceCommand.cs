using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.ApproveDevice;

/// <summary>
/// Approve a pending device-change request (BR-106): the requested device becomes Active,
/// the user's current active device is marked Replaced and its refresh tokens revoked so the
/// old app is logged out on its next refresh.
///
/// <paramref name="ApproverIsAdmin"/> = true bypasses scoping; otherwise the approver (a Leader)
/// may only approve devices belonging to PGs they actively manage (M03 user_leader_assignments).
/// </summary>
public sealed record ApproveDeviceCommand(Guid DeviceId, Guid ApproverUserId, bool ApproverIsAdmin) : IRequest<Result>;
