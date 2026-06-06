namespace Rmms.Api.Dtos.Approvals;

/// <summary>POST /approvals/{id}/reject — reason required (BR-404).</summary>
public sealed record RejectApprovalRequest(string Reason);

/// <summary>POST /admin/approvals/{id}/override — reason required + audited (BR-408).</summary>
public sealed record OverrideApprovalRequest(string Reason);

/// <summary>POST /approvals/email-action/confirm — public BUH decision via signed link (BR-407).</summary>
public sealed record EmailActionConfirmRequest(string Token, string Action, string? Reason);
