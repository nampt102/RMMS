namespace Rmms.Domain.Enums;

/// <summary>
/// Folder a document lives in (M13). Stored as snake_case string.
///   - <c>Public</c>: visible to anyone the document is assigned to (role/user).
///   - <c>Private</c>: sensitive (e.g. payslip) — assigned to a single user, short-lived URL.
/// </summary>
public enum DocumentFolderType
{
    Public = 1,
    Private = 2,
}
