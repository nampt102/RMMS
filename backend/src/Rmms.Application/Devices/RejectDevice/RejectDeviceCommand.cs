using Mediator;
using Rmms.Domain.Common;

namespace Rmms.Application.Devices.RejectDevice;

/// <summary>Reject a pending device-change request (BR-106). A reason is required.</summary>
public sealed record RejectDeviceCommand(Guid DeviceId, Guid ApproverUserId, string Reason) : IRequest<Result>;
