using Microsoft.AspNetCore.Http;

namespace Rmms.Api.Dtos.Attendance;

/// <summary>
/// POST /attendance/check-in — multipart/form-data. GPS + flags as form fields, selfie + store
/// photo as files. Photos are optional in Phase 1A (M06 Face / M13 MinIO not wired yet).
/// </summary>
public sealed class CheckInForm
{
    public Guid StoreId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public bool FakeGpsDetected { get; set; }
    public string? Note { get; set; }
    public IFormFile? Selfie { get; set; }
    public IFormFile? StorePhoto { get; set; }
}

/// <summary>POST /attendance/{id}/check-out — multipart/form-data (no store; bound to the open record).</summary>
public sealed class CheckOutForm
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public bool FakeGpsDetected { get; set; }
    public string? Note { get; set; }
    public IFormFile? Selfie { get; set; }
    public IFormFile? StorePhoto { get; set; }
}

/// <summary>POST /admin/attendance/{id}/review — approve or reject a pending-review record.</summary>
public sealed record ReviewAttendanceRequest(bool Approve, string? Note);
