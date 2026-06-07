namespace Rmms.Api.Dtos.LeaveOt;

/// <summary>POST /leave-requests — regular leave (date range, optional partial-day times).</summary>
public sealed record CreateLeaveRequestBody(
    DateOnly StartDate, DateOnly EndDate, TimeOnly? StartTime, TimeOnly? EndTime, string Reason);

/// <summary>POST /leave-requests/emergency — raised while checked in.</summary>
public sealed record EmergencyLeaveBody(string Reason);

/// <summary>POST /ot-requests — overtime (date + start/end time).</summary>
public sealed record CreateOtRequestBody(DateOnly OtDate, TimeOnly StartTime, TimeOnly EndTime, string Reason);
