using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Application.Common.Options;
using Rmms.Domain.Approvals;
using Rmms.Domain.Enums;
using Rmms.Infrastructure.Email;

namespace Rmms.Infrastructure.Approvals;

/// <summary>
/// Default <see cref="IApprovalService"/> (M09). Creates the approval row and, for a
/// BUH approver, issues a signed email-link token + sends the bilingual decision
/// email (BR-407). Entities are added to the context; the caller commits.
/// </summary>
internal sealed class ApprovalService : IApprovalService
{
    private readonly IAppDbContext _db;
    private readonly IApprovalTokenService _tokens;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;
    private readonly ApprovalOptions _approvalOptions;
    private readonly AppUrlOptions _appUrl;

    public ApprovalService(
        IAppDbContext db,
        IApprovalTokenService tokens,
        IEmailSender email,
        IDateTimeProvider clock,
        IOptions<ApprovalOptions> approvalOptions,
        IOptions<AppUrlOptions> appUrl)
    {
        _db = db;
        _tokens = tokens;
        _email = email;
        _clock = clock;
        _approvalOptions = approvalOptions.Value;
        _appUrl = appUrl.Value;
    }

    public async Task<Guid> CreateAsync(
        ApprovalEntityType entityType,
        Guid entityId,
        Guid requesterId,
        Guid approverId,
        UserRole approverRole,
        CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var approval = Approval.Create(entityType, entityId, requesterId, approverId, approverRole);
        _db.Approvals.Add(approval);

        // BR-407: BUH can decide via a signed, single-use email link (no login).
        if (approverRole == UserRole.Buh)
        {
            var issued = _tokens.Issue(approval.Id, approverId, new[] { "approve", "reject" }, now);
            var ttl = TimeSpan.FromHours(_approvalOptions.TokenTtlHours <= 0 ? 24 : _approvalOptions.TokenTtlHours);
            _db.ApprovalEmailTokens.Add(ApprovalEmailToken.Issue(approval.Id, issued.Hash, now, ttl));

            var approver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == approverId, ct);
            if (approver is not null)
            {
                var link = $"{_appUrl.AppBaseUrl.TrimEnd('/')}{_approvalOptions.WebApprovalPath}?token={issued.Token}";
                await _email.SendAsync(BuildEmail(approver.Email, approver.FullName, approver.PreferredLanguage, link), ct);
            }
        }

        return approval.Id;
    }

    private static EmailMessage BuildEmail(string toEmail, string toName, string lang, string link)
    {
        var isVi = !string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);
        var subject = isVi ? "RMMS — Yêu cầu phê duyệt" : "RMMS — Approval request";
        var text = isVi
            ? $"Xin chào {toName},\n\nBạn có một yêu cầu cần phê duyệt. Mở liên kết (hiệu lực 24 giờ, dùng một lần):\n{link}\n"
            : $"Hi {toName},\n\nYou have a request to approve. Open the link (valid 24h, single use):\n{link}\n";
        var html = isVi
            ? $"<p>Xin chào {toName},</p><p>Bạn có một yêu cầu cần phê duyệt.</p><p><a href=\"{link}\">Mở để phê duyệt</a> (hiệu lực 24 giờ, dùng một lần).</p>"
            : $"<p>Hi {toName},</p><p>You have a request to approve.</p><p><a href=\"{link}\">Open to decide</a> (valid 24h, single use).</p>";
        return new EmailMessage(toEmail, toName, subject, text, html, isVi ? "vi" : "en");
    }
}
