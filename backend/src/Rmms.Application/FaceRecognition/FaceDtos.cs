namespace Rmms.Application.FaceRecognition;

/// <summary>Face enrollment status for a user (M06).</summary>
public sealed record FaceStatusDto(bool Enrolled, DateTimeOffset? EnrolledAt);

/// <summary>Result of a standalone face verify call.</summary>
public sealed record FaceVerifyResponse(string Result, decimal? Confidence);
