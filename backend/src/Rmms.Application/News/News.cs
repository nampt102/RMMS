using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.News;
using Rmms.Shared.Errors;

namespace Rmms.Application.News;

// ===== Create (admin draft) =====

public sealed record CreateNewsCommand(
    string TitleVi, string TitleEn, string ContentVi, string ContentEn, string? Category, bool IsImportant)
    : IRequest<Result<Guid>>;

public sealed class CreateNewsCommandValidator : AbstractValidator<CreateNewsCommand>
{
    public CreateNewsCommandValidator()
    {
        RuleFor(x => x.TitleVi).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.TitleEn).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
    }
}

internal sealed class CreateNewsCommandHandler : IRequestHandler<CreateNewsCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateNewsCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateNewsCommand command, CancellationToken ct)
    {
        var news = NewsItem.Create(
            command.TitleVi, command.TitleEn, command.ContentVi, command.ContentEn, command.Category, command.IsImportant);
        _db.News.Add(news);
        await _audit.RecordAsync(AuditAction.NewsCreated, "news", news.Id, new { news.TitleVi, news.IsImportant }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(news.Id);
    }
}

// ===== Update (admin) =====

public sealed record UpdateNewsCommand(
    Guid Id, string TitleVi, string TitleEn, string ContentVi, string ContentEn, string? Category, bool IsImportant)
    : IRequest<Result>;

internal sealed class UpdateNewsCommandHandler : IRequestHandler<UpdateNewsCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateNewsCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateNewsCommand command, CancellationToken ct)
    {
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == command.Id, ct);
        if (news is null) return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tin tức."));

        if (string.IsNullOrWhiteSpace(command.TitleVi) || string.IsNullOrWhiteSpace(command.TitleEn))
        {
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Tiêu đề (vi/en) là bắt buộc."));
        }

        news.UpdateContent(command.TitleVi, command.TitleEn, command.ContentVi, command.ContentEn, command.Category, command.IsImportant);
        await _audit.RecordAsync(AuditAction.NewsUpdated, "news", news.Id, new { news.TitleVi }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Assign (admin) =====

public sealed record AssignNewsCommand(Guid NewsId, string? Role, Guid? UserId) : IRequest<Result<Guid>>;

internal sealed class AssignNewsCommandHandler : IRequestHandler<AssignNewsCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public AssignNewsCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(AssignNewsCommand command, CancellationToken ct)
    {
        if (!await _db.News.AnyAsync(n => n.Id == command.NewsId, ct))
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tin tức."));
        }
        if (command.Role is null && command.UserId is null)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Cần chọn role hoặc người dùng."));
        }
        if (command.Role is not null && command.Role is not ("pg" or "leader"))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Role chỉ nhận pg/leader."));
        }

        var assignment = NewsAssignment.Create(command.NewsId, command.Role, command.UserId);
        _db.NewsAssignments.Add(assignment);
        await _audit.RecordAsync(AuditAction.NewsAssigned, "news", command.NewsId, new { command.Role, command.UserId }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(assignment.Id);
    }
}

// ===== Publish (admin) — notifies recipients (CR-2) =====

public sealed record PublishNewsCommand(Guid NewsId, Guid PublishedBy) : IRequest<Result>;

internal sealed class PublishNewsCommandHandler : IRequestHandler<PublishNewsCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifier;

    public PublishNewsCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock, INotificationService notifier)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
        _notifier = notifier;
    }

    public async ValueTask<Result> Handle(PublishNewsCommand command, CancellationToken ct)
    {
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == command.NewsId, ct);
        if (news is null) return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tin tức."));
        if (news.IsPublished) return Result.Failure(Error.Conflict(ErrorCodes.Conflict, "Tin tức đã được phát hành."));

        news.Publish(command.PublishedBy, _clock.UtcNow);
        await _audit.RecordAsync(AuditAction.NewsPublished, "news", news.Id, new { news.TitleVi, news.IsImportant }, ct);

        var recipients = await NewsRecipients.ResolveAsync(_db, news.Id, ct);
        var data = new Dictionary<string, string>
        {
            ["deepLink"] = $"rmms://news/{news.Id}",
            ["newsId"] = news.Id.ToString(),
        };
        var spec = new NotificationSpec(
            NotificationType.News,
            TitleVi: news.IsImportant ? $"[Quan trọng] {news.TitleVi}" : news.TitleVi,
            TitleEn: news.IsImportant ? $"[Important] {news.TitleEn}" : news.TitleEn,
            BodyVi: news.IsImportant ? "Tin quan trọng — vui lòng xác nhận đã đọc." : "Có tin tức mới.",
            BodyEn: news.IsImportant ? "Important news — please confirm you have read it." : "A new announcement is available.",
            Data: data, Push: true, Email: news.IsImportant);

        foreach (var uid in recipients)
        {
            await _notifier.NotifyAsync(uid, spec, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Delete (admin, soft) =====

public sealed record DeleteNewsCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteNewsCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteNewsCommand command, CancellationToken ct)
    {
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == command.Id, ct);
        if (news is null) return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tin tức."));

        _db.News.Remove(news); // soft-delete (ADR-004)
        await _audit.RecordAsync(AuditAction.NewsDeleted, "news", news.Id, new { news.TitleVi }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== My news (mobile) — published + assigned, with read state =====

public sealed record GetMyNewsQuery(Guid ViewerId, UserRole ViewerRole) : IRequest<Result<IReadOnlyList<NewsDto>>>;

internal sealed class GetMyNewsQueryHandler : IRequestHandler<GetMyNewsQuery, Result<IReadOnlyList<NewsDto>>>
{
    private readonly IAppDbContext _db;

    public GetMyNewsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<NewsDto>>> Handle(GetMyNewsQuery query, CancellationToken ct)
    {
        var ids = await NewsScope.ResolveAssignedIdsAsync(_db, query.ViewerId, query.ViewerRole, ct);

        var news = await _db.News.AsNoTracking()
            .Where(n => ids.Contains(n.Id) && n.PublishedAt != null)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync(ct);

        var reads = await _db.NewsReads.AsNoTracking()
            .Where(r => r.UserId == query.ViewerId && ids.Contains(r.NewsId))
            .ToListAsync(ct);
        var readByNews = reads.ToDictionary(r => r.NewsId);

        var items = news.Select(n => NewsMapper.ToDto(n, readByNews.GetValueOrDefault(n.Id))).ToList();
        return Result.Success<IReadOnlyList<NewsDto>>(items);
    }
}

// ===== Mark read =====

public sealed record MarkNewsReadCommand(Guid NewsId, Guid UserId) : IRequest<Result>;

internal sealed class MarkNewsReadCommandHandler : IRequestHandler<MarkNewsReadCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MarkNewsReadCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(MarkNewsReadCommand command, CancellationToken ct)
    {
        var existing = await _db.NewsReads
            .FirstOrDefaultAsync(r => r.NewsId == command.NewsId && r.UserId == command.UserId, ct);
        if (existing is null)
        {
            _db.NewsReads.Add(NewsRead.Create(command.NewsId, command.UserId, _clock.UtcNow));
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}

// ===== Confirm important news =====

public sealed record ConfirmNewsCommand(Guid NewsId, Guid UserId) : IRequest<Result>;

internal sealed class ConfirmNewsCommandHandler : IRequestHandler<ConfirmNewsCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ConfirmNewsCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result> Handle(ConfirmNewsCommand command, CancellationToken ct)
    {
        var news = await _db.News.AsNoTracking().FirstOrDefaultAsync(n => n.Id == command.NewsId, ct);
        if (news is null) return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tin tức."));
        if (!news.IsImportant)
        {
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Tin này không yêu cầu xác nhận."));
        }

        var read = await _db.NewsReads.FirstOrDefaultAsync(r => r.NewsId == command.NewsId && r.UserId == command.UserId, ct);
        if (read is null)
        {
            read = NewsRead.Create(command.NewsId, command.UserId, _clock.UtcNow);
            _db.NewsReads.Add(read);
        }
        read.Confirm(_clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Admin list =====

public sealed record AdminGetNewsQuery : IRequest<Result<IReadOnlyList<AdminNewsDto>>>;

internal sealed class AdminGetNewsQueryHandler : IRequestHandler<AdminGetNewsQuery, Result<IReadOnlyList<AdminNewsDto>>>
{
    private readonly IAppDbContext _db;

    public AdminGetNewsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<AdminNewsDto>>> Handle(AdminGetNewsQuery query, CancellationToken ct)
    {
        var news = await _db.News.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<AdminNewsDto>>(news.Select(NewsMapper.ToAdminDto).ToList());
    }
}

// ----- Helpers -----

internal static class NewsScope
{
    /// <summary>News ids assigned to the viewer (role-scoped OR direct user, OR logic).</summary>
    public static async Task<HashSet<Guid>> ResolveAssignedIdsAsync(
        IAppDbContext db, Guid viewerId, UserRole viewerRole, CancellationToken ct)
    {
        var roleStr = viewerRole.ToString().ToLowerInvariant();
        var ids = await db.NewsAssignments.AsNoTracking()
            .Where(a => a.AssignedToUserId == viewerId || a.AssignedToRole == roleStr)
            .Select(a => a.NewsId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}

internal static class NewsRecipients
{
    /// <summary>Resolve the distinct user ids a news item is assigned to (role users + direct users).</summary>
    public static async Task<List<Guid>> ResolveAsync(IAppDbContext db, Guid newsId, CancellationToken ct)
    {
        var rules = await db.NewsAssignments.AsNoTracking()
            .Where(a => a.NewsId == newsId)
            .Select(a => new { a.AssignedToRole, a.AssignedToUserId })
            .ToListAsync(ct);

        var users = new HashSet<Guid>();
        var roles = new HashSet<UserRole>();
        foreach (var r in rules)
        {
            if (r.AssignedToUserId is { } uid) users.Add(uid);
            if (r.AssignedToRole is { } role && Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
            {
                roles.Add(parsed);
            }
        }

        if (roles.Count > 0)
        {
            var roleUsers = await db.Users.AsNoTracking()
                .Where(u => u.Status == UserStatus.Active && roles.Contains(u.Role))
                .Select(u => u.Id)
                .ToListAsync(ct);
            foreach (var id in roleUsers) users.Add(id);
        }

        return users.ToList();
    }
}
