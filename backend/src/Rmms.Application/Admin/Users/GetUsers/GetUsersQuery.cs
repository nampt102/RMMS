using Mediator;
using Rmms.Application.Admin.Users;
using Rmms.Domain.Common;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Admin.Users.GetUsers;

/// <summary>
/// Paginated admin user list. Filters: role, status, search (email + full_name).
/// </summary>
public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Role = null,        // "pg" | "leader" | "buh" | "admin"
    string? Status = null,      // "active" | "inactive" | "pending_email_verify"
    string? Search = null)
    : IRequest<Result<PaginatedResponse<AdminUserDto>>>;
