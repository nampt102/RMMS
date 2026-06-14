namespace Rmms.Api.Dtos.VisitPlans;

/// <summary>One planned store visit in a create/edit request (M11).</summary>
public sealed record VisitPlanItemRequest(Guid StoreId, Guid FormId);

/// <summary>Leader creates a visit plan (M11). Items must be non-empty.</summary>
public sealed record CreateVisitPlanRequest(
    DateOnly VisitDate,
    string? Notes,
    IReadOnlyList<VisitPlanItemRequest> Items);

/// <summary>Leader edits a still-pending visit plan (M11).</summary>
public sealed record EditVisitPlanRequest(
    DateOnly VisitDate,
    string? Notes,
    IReadOnlyList<VisitPlanItemRequest> Items);

/// <summary>Link a post-visit form submission to a plan item (M11).</summary>
public sealed record ExecuteVisitItemRequest(Guid FormSubmissionId);
