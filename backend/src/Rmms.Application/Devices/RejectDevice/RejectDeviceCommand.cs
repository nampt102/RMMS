using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.RejectDevice;

/// <summary>
/// Reject a pending device-change request (BR-106). A reason is required.
/// <paramref name="ApproverIsAdmin"/> = true bypasses scoping; otherwise the approver (a Leader)
/// may only reject devices of PGs they actively manage (M03 user_leader_assignments).
/// </summary>
public sealed record RejectDeviceCommand(Guid DeviceId, Guid ApproverUserId, string Reason, bool ApproverIsAdmin) : IRequest<Result>;
