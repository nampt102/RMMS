using System.ComponentModel.DataAnnotations;

namespace Rmms.Api.Dtos.Devices;

/// <summary>Body for <c>POST /api/v1/devices/{id}/reject</c>.</summary>
public sealed record RejectDeviceRequest([Required][MaxLength(500)] string Reason);
