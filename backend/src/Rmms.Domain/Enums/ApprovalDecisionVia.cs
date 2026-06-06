namespace Rmms.Domain.Enums;

/// <summary>
/// Channel a decision was made through (M09). Stored as snake_case string
/// (<c>app</c> / <c>web</c> / <c>email_link</c>). BR-407 allows BUH to decide via
/// a signed email link without logging in.
/// </summary>
public enum ApprovalDecisionVia
{
    App = 1,
    Web = 2,
    EmailLink = 3,
}
