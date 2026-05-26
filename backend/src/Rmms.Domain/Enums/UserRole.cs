namespace Rmms.Domain.Enums;

/// <summary>
/// User roles per <c>knowledge-base/01-glossary.md</c>.
/// Stored as lowercase string in DB (see 05-api-conventions.md "Enums").
/// </summary>
public enum UserRole
{
    Pg = 1,        // Promotion Girl/Boy — mobile primary
    Leader = 2,    // PG's direct manager
    Buh = 3,       // Business Unit Head — Leader's manager
    Admin = 4,     // System administrator
}
