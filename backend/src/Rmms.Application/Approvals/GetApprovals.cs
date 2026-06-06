using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Approvals;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Approvals;

/// <summary>Pending approvals routed to the current user (Leader/BUH queue) — M09 AC-17.</summary>
public sealed record GetPendingApprovalsQuery(Guid ApproverId) : IRequest<Result<IReadOnlyList<ApprovalDto>>>;

/// <summary>Single approval detail; visible to its requester, its approver, or an Admin.</summary>
public sealed record GetApprovalDetailQuery(Guid Id, Guid CurrentUserId, bool IsAdmin)
    : IRequest<Result<ApprovalDto>>;

internal sealed class GetPendingApprovalsQueryHandler
    : IRequestHandler<GetPendingApprovalsQuery, Result<IReadOnlyList<ApprovalDto>>>
{
    private readonly IAppDbContext _db;

    public GetPendingApprovalsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<ApprovalDto>>> Handle(GetPendingApprovalsQuery query, CancellationToken ct)
    {
        var rows = await _db.Approvals.AsNoTracking()
            .Where(a => a.ApproverId == query.ApproverId && a.Status == ApprovalStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .Join(_db.Users.AsNoTracking(), a => a.RequesterId, u => u.Id, (a, u) => new { a, u.FullName })
            .ToListAsync(ct);

        IReadOnlyList<ApprovalDto> dtos = rows
            .Select(x => ApprovalMapper.ToDto(x.a, x.FullName))
            .ToList();
        return Result.Success(dtos);
    }
}

internal sealed class GetApprovalDetailQueryHandler
    : IRequestHandler<GetApprovalDetailQuery, Result<ApprovalDto>>
{
    private readonly IAppDbContext _db;

    public GetApprovalDetailQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<ApprovalDto>> Handle(GetApprovalDetailQuery query, CancellationToken ct)
    {
        var approval = await _db.Approvals.AsNoTracking().FirstOrDefaultAsync(a => a.Id == query.Id, ct);
        if (approval is null)
            return Result.Failure<ApprovalDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy yêu cầu phê duyệt."));

        var visible = query.IsAdmin
            || approval.ApproverId == query.CurrentUserId
            || approval.RequesterId == query.CurrentUserId;
        if (!visible)
            return Result.Failure<ApprovalDto>(Error.Forbidden(ErrorCodes.PermissionDenied, "Bạn không có quyền xem yêu cầu này."));

        var requesterName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == approval.RequesterId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return Result.Success(ApprovalMapper.ToDto(approval, requesterName));
    }
}
