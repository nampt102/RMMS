using Microsoft.AspNetCore.Http;

namespace Rmms.Api.Dtos.Attendance;

/// <summary>
/// POST /attendance/check-in — multipart/form-data. GPS + flags as form fields, selfie + store
/// photo as files. Photos are optional in Phase 1A (M06 Face / M13 MinIO not wired yet).
///
/// GPS fields (<c>latitude</c> / <c>longitude</c> / <c>accuracyMeters</c>) are read straight
/// from <c>Request.Form</c> with invariant culture (see <c>InvariantFormParsing</c>) — model
/// binding would corrupt them under the vi-VN request culture, so they are NOT bound here.
/// </summary>
public sealed class CheckInForm
{
    public Guid StoreId { get; set; }
    public bool FakeGpsDetected { get; set; }
    public string? Note { get; set; }
    public IFormFile? Selfie { get; set; }
    public IFormFile? StorePhoto { get; set; }
}

/// <summary>
/// POST /attendance/{id}/check-out — multipart/form-data (no store; bound to the open record).
/// GPS fields are read via <c>InvariantFormParsing</c>, not model-bound (see <see cref="CheckInForm"/>).
/// </summary>
public sealed class CheckOutForm
{
    public bool FakeGpsDetected { get; set; }
    public string? Note { get; set; }
    public IFormFile? Selfie { get; set; }
    public IFormFile? StorePhoto { get; set; }
}

/// <summary>POST /admin/attendance/{id}/review — approve or reject a pending-review record.</summary>
public sealed record ReviewAttendanceRequest(bool Approve, string? Note);
