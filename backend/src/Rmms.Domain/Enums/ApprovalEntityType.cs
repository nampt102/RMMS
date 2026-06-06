namespace Rmms.Domain.Enums;

/// <summary>
/// The kind of request an <c>approvals</c> row gates (M09). Stored as snake_case
/// string. The approval engine is generic; Phase 1 produces <c>work_schedule</c>
/// approvals, with OT / Leave / Visit Plan reserved for later phases.
/// </summary>
public enum ApprovalEntityType
{
    WorkSchedule = 1,
    OtRequest = 2,
    LeaveRequest = 3,
    VisitPlan = 4,
}
