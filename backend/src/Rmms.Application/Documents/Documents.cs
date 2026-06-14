using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Documents;
using Rmms.Domain.Enums;
using Rmms.Shared.Errors;

namespace Rmms.Application.Documents;

// ===== Upload (admin) =====

public sealed record UploadDocumentCommand(
    string Name, string? Description, string FolderType, PhotoUpload File, Guid UploadedBy)
    : IRequest<Result<Guid>>;

internal sealed class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAttendancePhotoStorage _storage;
    private readonly IAuditLogger _audit;

    public UploadDocumentCommandHandler(IAppDbContext db, IAttendancePhotoStorage storage, IAuditLogger audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(UploadDocumentCommand command, CancellationToken ct)
    {
        if (!DocumentFolders.TryParse(command.FolderType, out var folder))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "folderType chỉ nhận public/private."));
        }
        if (command.File.Content.Length == 0)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Tệp rỗng."));
        }

        // Random object key under a documents prefix (never a predictable filename — M13 note).
        var key = await _storage.SaveAsync(command.UploadedBy, "documents", command.File, ct);

        var doc = Document.Create(
            command.Name, command.Description, folder, key,
            command.File.Content.Length, command.File.ContentType, command.UploadedBy);
        _db.Documents.Add(doc);

        await _audit.RecordAsync(AuditAction.DocumentUploaded, "document", doc.Id,
            new { doc.Name, folder = folder.ToSnakeCase(), doc.FileSizeBytes }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(doc.Id);
    }
}

// ===== Assign (admin) — notifies target users (CR-2) =====

public sealed record AssignDocumentCommand(Guid DocumentId, string? Role, Guid? UserId) : IRequest<Result<Guid>>;

internal sealed class AssignDocumentCommandHandler : IRequestHandler<AssignDocumentCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notifier;

    public AssignDocumentCommandHandler(IAppDbContext db, IAuditLogger audit, INotificationService notifier)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
    }

    public async ValueTask<Result<Guid>> Handle(AssignDocumentCommand command, CancellationToken ct)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == command.DocumentId, ct);
        if (doc is null)
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tài liệu."));
        }
        if (command.Role is null && command.UserId is null)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Cần chọn role hoặc người dùng."));
        }
        if (command.Role is not null && command.Role is not ("pg" or "leader"))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Role chỉ nhận pg/leader."));
        }

        var assignment = DocumentAssignment.Create(command.DocumentId, command.Role, command.UserId);
        _db.DocumentAssignments.Add(assignment);

        await _audit.RecordAsync(AuditAction.DocumentAssigned, "document", doc.Id,
            new { command.Role, command.UserId }, ct);

        // Notify recipients (CR-2: new document / payslip). Bounded fan-out in a single-customer system.
        var recipients = await ResolveRecipientsAsync(command.Role, command.UserId, ct);
        var spec = BuildSpec(doc);
        foreach (var uid in recipients)
        {
            await _notifier.NotifyAsync(uid, spec, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(assignment.Id);
    }

    private async Task<List<Guid>> ResolveRecipientsAsync(string? role, Guid? userId, CancellationToken ct)
    {
        if (userId is { } uid) return new List<Guid> { uid };
        if (role is null || !Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole)) return new List<Guid>();

        return await _db.Users.AsNoTracking()
            .Where(u => u.Status == UserStatus.Active && u.Role == parsedRole)
            .Select(u => u.Id)
            .ToListAsync(ct);
    }

    private static NotificationSpec BuildSpec(Document doc)
    {
        var data = new Dictionary<string, string>
        {
            ["deepLink"] = $"rmms://documents/{doc.Id}",
            ["documentId"] = doc.Id.ToString(),
        };
        return doc.IsPrivate
            ? new NotificationSpec(NotificationType.Payslip,
                TitleVi: "Bạn có tài liệu riêng mới", TitleEn: "You have a new private document",
                BodyVi: doc.Name, BodyEn: doc.Name, Data: data, Push: true, Email: true)
            : new NotificationSpec(NotificationType.Document,
                TitleVi: "Tài liệu mới", TitleEn: "New document",
                BodyVi: doc.Name, BodyEn: doc.Name, Data: data, Push: true, Email: false);
    }
}

// ===== My documents (mobile) =====

public sealed record GetMyDocumentsQuery(Guid ViewerId, UserRole ViewerRole, string? Search)
    : IRequest<Result<IReadOnlyList<DocumentDto>>>;

internal sealed class GetMyDocumentsQueryHandler : IRequestHandler<GetMyDocumentsQuery, Result<IReadOnlyList<DocumentDto>>>
{
    private readonly IAppDbContext _db;

    public GetMyDocumentsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<DocumentDto>>> Handle(GetMyDocumentsQuery query, CancellationToken ct)
    {
        var ids = await DocumentScope.ResolveAccessibleIdsAsync(_db, query.ViewerId, query.ViewerRole, ct);

        var docs = await _db.Documents.AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        // Name search filtered in memory (small set) — avoids provider-specific case-insensitive SQL.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            docs = docs.Where(d => d.Name.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Result.Success<IReadOnlyList<DocumentDto>>(docs.Select(DocumentMapper.ToDto).ToList());
    }
}

// ===== Download (signed URL) =====

public sealed record GetDocumentDownloadUrlQuery(Guid DocumentId, Guid ViewerId, UserRole ViewerRole)
    : IRequest<Result<string>>;

internal sealed class GetDocumentDownloadUrlQueryHandler : IRequestHandler<GetDocumentDownloadUrlQuery, Result<string>>
{
    private readonly IAppDbContext _db;
    private readonly IAttendancePhotoStorage _storage;
    private readonly IAuditLogger _audit;

    public GetDocumentDownloadUrlQueryHandler(IAppDbContext db, IAttendancePhotoStorage storage, IAuditLogger audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async ValueTask<Result<string>> Handle(GetDocumentDownloadUrlQuery query, CancellationToken ct)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == query.DocumentId, ct);
        if (doc is null)
        {
            return Result.Failure<string>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tài liệu."));
        }

        var allowed = query.ViewerRole == UserRole.Admin
            || (await DocumentScope.ResolveAccessibleIdsAsync(_db, query.ViewerId, query.ViewerRole, ct)).Contains(doc.Id);
        if (!allowed)
        {
            return Result.Failure<string>(Error.Forbidden(ErrorCodes.PermissionDenied, "Bạn không có quyền tải tài liệu này."));
        }

        var url = await _storage.GetUrlAsync(doc.FileKey, ct);
        if (string.IsNullOrEmpty(url))
        {
            return Result.Failure<string>(Error.Conflict(ErrorCodes.UpstreamUnavailable, "Không tạo được liên kết tải."));
        }

        // Audit private downloads (CR-1).
        if (doc.IsPrivate)
        {
            await _audit.RecordAsync(AuditAction.DocumentDownloaded, "document", doc.Id, new { by = query.ViewerId }, ct);
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success(url);
    }
}

// ===== Delete (admin) =====

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;

internal sealed class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteDocumentCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteDocumentCommand command, CancellationToken ct)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == command.DocumentId, ct);
        if (doc is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy tài liệu."));
        }

        _db.Documents.Remove(doc); // soft-delete via interceptor (ADR-004)
        await _audit.RecordAsync(AuditAction.DocumentDeleted, "document", doc.Id, new { doc.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Admin list =====

public sealed record AdminGetDocumentsQuery(string? FolderType) : IRequest<Result<IReadOnlyList<DocumentDto>>>;

internal sealed class AdminGetDocumentsQueryHandler : IRequestHandler<AdminGetDocumentsQuery, Result<IReadOnlyList<DocumentDto>>>
{
    private readonly IAppDbContext _db;

    public AdminGetDocumentsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<DocumentDto>>> Handle(AdminGetDocumentsQuery query, CancellationToken ct)
    {
        var q = _db.Documents.AsNoTracking().AsQueryable();
        if (DocumentFolders.TryParse(query.FolderType, out var folder))
        {
            q = q.Where(d => d.FolderType == folder);
        }

        var docs = await q.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
        return Result.Success<IReadOnlyList<DocumentDto>>(docs.Select(DocumentMapper.ToDto).ToList());
    }
}

// ----- Helpers -----

internal static class DocumentFolders
{
    public static bool TryParse(string? value, out DocumentFolderType folder)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "public": folder = DocumentFolderType.Public; return true;
            case "private": folder = DocumentFolderType.Private; return true;
            default: folder = default; return false;
        }
    }
}

internal static class DocumentScope
{
    /// <summary>Document ids the viewer can access now: role-scoped OR directly user-assigned (OR logic).</summary>
    public static async Task<HashSet<Guid>> ResolveAccessibleIdsAsync(
        IAppDbContext db, Guid viewerId, UserRole viewerRole, CancellationToken ct)
    {
        var roleStr = viewerRole.ToString().ToLowerInvariant();
        var ids = await db.DocumentAssignments.AsNoTracking()
            .Where(a => a.AssignedToUserId == viewerId || a.AssignedToRole == roleStr)
            .Select(a => a.DocumentId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}
