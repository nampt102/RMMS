namespace Rmms.Api.Dtos.Documents;

/// <summary>Grant access to a document by role or single user (M13). Payslip = single user.</summary>
public sealed record AssignDocumentRequest(string? Role, Guid? UserId);
