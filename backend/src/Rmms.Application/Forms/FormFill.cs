using System.Text.Json;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Forms;
using Rmms.Shared.Errors;

namespace Rmms.Application.Forms;

// ===== Assign (admin) =====

public sealed record AssignFormCommand(
    Guid FormId, string? Role, Guid? UserId, Guid? StoreId, Guid? AreaId, Guid? CategoryId, Guid? ProductId,
    DateTimeOffset? ValidFrom, DateTimeOffset? ValidTo) : IRequest<Result<Guid>>;

internal sealed class AssignFormCommandHandler : IRequestHandler<AssignFormCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public AssignFormCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<Guid>> Handle(AssignFormCommand command, CancellationToken ct)
    {
        if (!await _db.Forms.AnyAsync(f => f.Id == command.FormId, ct))
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }

        var hasTarget = command.Role is not null || command.UserId is not null || command.StoreId is not null
            || command.AreaId is not null || command.CategoryId is not null || command.ProductId is not null;
        if (!hasTarget)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Cần ít nhất một đối tượng phân công."));
        }

        if (command.Role is not null && command.Role is not ("pg" or "leader"))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Role chỉ nhận pg/leader."));
        }

        var assignment = FormAssignment.Create(
            command.FormId, command.Role, command.UserId, command.StoreId, command.AreaId,
            command.CategoryId, command.ProductId, command.ValidFrom ?? _clock.UtcNow, command.ValidTo);
        _db.FormAssignments.Add(assignment);

        await _audit.RecordAsync(AuditAction.FormAssigned, "form", command.FormId, new { command.Role, command.UserId, command.StoreId, command.CategoryId }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(assignment.Id);
    }
}

// ===== Attachment upload (mobile, image/file fields) =====

public sealed record FormAttachmentDto(string ObjectKey, string? Url);

public sealed record UploadFormAttachmentCommand(Guid FormId, Guid UserId, PhotoUpload File)
    : IRequest<Result<FormAttachmentDto>>;

internal sealed class UploadFormAttachmentCommandHandler
    : IRequestHandler<UploadFormAttachmentCommand, Result<FormAttachmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IAttendancePhotoStorage _storage;

    public UploadFormAttachmentCommandHandler(IAppDbContext db, IAttendancePhotoStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async ValueTask<Result<FormAttachmentDto>> Handle(UploadFormAttachmentCommand command, CancellationToken ct)
    {
        if (!await _db.Forms.AnyAsync(f => f.Id == command.FormId, ct))
        {
            return Result.Failure<FormAttachmentDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }
        if (command.File.Content.Length == 0)
        {
            return Result.Failure<FormAttachmentDto>(Error.Validation(ErrorCodes.ValidationFailed, "Tệp rỗng."));
        }

        // Reuse the object store (MinIO); attachments live under a form-scoped key.
        var key = await _storage.SaveAsync(command.UserId, $"form-{command.FormId:N}", command.File, ct);
        var url = await _storage.GetUrlAsync(key, ct);
        return Result.Success(new FormAttachmentDto(key, url));
    }
}

// ===== My forms (mobile) =====

public sealed record AssignedFormDto(
    Guid FormId, string Code, string NameVi, string NameEn, string FormType, int Version, DateTimeOffset? ValidTo);

public sealed record GetMyFormsQuery(Guid ViewerId, UserRole ViewerRole) : IRequest<Result<IReadOnlyList<AssignedFormDto>>>;

internal sealed class GetMyFormsQueryHandler : IRequestHandler<GetMyFormsQuery, Result<IReadOnlyList<AssignedFormDto>>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetMyFormsQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<IReadOnlyList<AssignedFormDto>>> Handle(GetMyFormsQuery query, CancellationToken ct)
    {
        var resolved = await FormScope.ResolveAssignedFormIdsAsync(_db, _clock, query.ViewerId, query.ViewerRole, ct);

        var forms = await _db.Forms.AsNoTracking()
            .Where(f => resolved.formIds.Contains(f.Id) && f.Status == FormStatus.Published && f.CurrentVersion > 0)
            .Select(f => new { f.Id, f.Code, f.NameVi, f.NameEn, f.FormType, f.CurrentVersion })
            .ToListAsync(ct);

        var items = forms.Select(f => new AssignedFormDto(
            f.Id, f.Code, f.NameVi, f.NameEn, FormTypeMap.ToText(f.FormType), f.CurrentVersion,
            resolved.validTo.GetValueOrDefault(f.Id))).ToList();

        return Result.Success<IReadOnlyList<AssignedFormDto>>(items);
    }
}

// ===== Get form to fill (mobile) =====

public sealed record FormFillDto(
    Guid FormId, string Code, string NameVi, string NameEn, string FormType, int Version, string Schema);

public sealed record GetFormForFillQuery(Guid FormId, Guid ViewerId, UserRole ViewerRole) : IRequest<Result<FormFillDto>>;

internal sealed class GetFormForFillQueryHandler : IRequestHandler<GetFormForFillQuery, Result<FormFillDto>>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetFormForFillQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async ValueTask<Result<FormFillDto>> Handle(GetFormForFillQuery query, CancellationToken ct)
    {
        var form = await _db.Forms.AsNoTracking().SingleOrDefaultAsync(f => f.Id == query.FormId, ct);
        if (form is null)
        {
            return Result.Failure<FormFillDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }
        if (form.Status != FormStatus.Published || form.CurrentVersion <= 0)
        {
            return Result.Failure<FormFillDto>(Error.Validation(ErrorCodes.FormExpired, "Form chưa được phát hành."));
        }

        var resolved = await FormScope.ResolveAssignedFormIdsAsync(_db, _clock, query.ViewerId, query.ViewerRole, ct);
        if (!resolved.formIds.Contains(form.Id))
        {
            return Result.Failure<FormFillDto>(Error.Validation(ErrorCodes.FormNotAssigned, "Form chưa được phân công cho bạn."));
        }

        var version = await _db.FormVersions.AsNoTracking()
            .SingleOrDefaultAsync(v => v.FormId == form.Id && v.Version == form.CurrentVersion, ct);
        if (version is null)
        {
            return Result.Failure<FormFillDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy phiên bản form."));
        }

        return Result.Success(new FormFillDto(
            form.Id, form.Code, form.NameVi, form.NameEn, FormTypeMap.ToText(form.FormType), version.Version, version.Schema));
    }
}

// ===== Submit (mobile) =====

public sealed record SubmitFormCommand(
    Guid FormId, Guid ViewerId, UserRole ViewerRole, string Answers, string? Attachments,
    Guid? StoreId, int TimeSpentSeconds, string ClientIdempotencyKey) : IRequest<Result<Guid>>;

public sealed class SubmitFormCommandValidator : AbstractValidator<SubmitFormCommand>
{
    public SubmitFormCommandValidator()
    {
        RuleFor(x => x.ClientIdempotencyKey).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(100);
        RuleFor(x => x.Answers).NotEmpty().WithErrorCode("REQUIRED");
    }
}

internal sealed class SubmitFormCommandHandler : IRequestHandler<SubmitFormCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public SubmitFormCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async ValueTask<Result<Guid>> Handle(SubmitFormCommand command, CancellationToken ct)
    {
        // Offline-retry dedup (AC-23): same (user, client key) returns the existing submission.
        var existing = await _db.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == command.ViewerId && s.ClientIdempotencyKey == command.ClientIdempotencyKey, ct);
        if (existing is not null)
        {
            return Result.Success(existing.Id);
        }

        var form = await _db.Forms.AsNoTracking().SingleOrDefaultAsync(f => f.Id == command.FormId, ct);
        if (form is null)
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }
        if (form.Status != FormStatus.Published || form.CurrentVersion <= 0)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.FormExpired, "Form chưa được phát hành."));
        }

        var resolved = await FormScope.ResolveAssignedFormIdsAsync(_db, _clock, command.ViewerId, command.ViewerRole, ct);
        if (!resolved.formIds.Contains(form.Id))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.FormNotAssigned, "Form chưa được phân công cho bạn."));
        }

        var version = await _db.FormVersions.AsNoTracking()
            .SingleOrDefaultAsync(v => v.FormId == form.Id && v.Version == form.CurrentVersion, ct);
        if (version is null)
        {
            return Result.Failure<Guid>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy phiên bản form."));
        }

        if (!FormAnswers.IsValidJson(command.Answers))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Answers phải là JSON hợp lệ."));
        }

        var missing = FormAnswers.FindMissingRequired(version.Schema, command.Answers);
        if (missing is not null)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, $"Thiếu trường bắt buộc: {missing}."));
        }

        var submission = FormSubmission.Create(
            form.Id, version.Id, command.ViewerId, command.StoreId, command.Answers, command.Attachments,
            score: null, command.TimeSpentSeconds, command.ClientIdempotencyKey, _clock.UtcNow);
        _db.FormSubmissions.Add(submission);

        await _audit.RecordAsync(AuditAction.FormSubmitted, "form_submission", submission.Id, new { form.Code, Version = version.Version }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(submission.Id);
    }
}

// ----- Assignment resolution (OR logic, design doc §4) -----

internal static class FormScope
{
    /// <summary>
    /// Resolve the set of form ids assigned to a viewer right now (role / direct user / their stores /
    /// their categories), plus each form's assignment validTo. Area/product targeting is deferred.
    /// Assignments are small, so we filter in memory to stay EF-translation-safe.
    /// </summary>
    public static async Task<(HashSet<Guid> formIds, Dictionary<Guid, DateTimeOffset?> validTo)> ResolveAssignedFormIdsAsync(
        IAppDbContext db, IDateTimeProvider clock, Guid viewerId, UserRole viewerRole, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime); // CR-5
        var roleStr = viewerRole.ToString().ToLowerInvariant();

        var storeIds = (await db.UserStoreAssignments.AsNoTracking()
            .Where(a => a.UserId == viewerId && (a.EffectiveTo == null || a.EffectiveTo >= today))
            .Select(a => a.StoreId).ToListAsync(ct)).ToHashSet();

        var categoryIds = (await db.UserCategoryAssignments.AsNoTracking()
            .Where(a => a.UserId == viewerId)
            .Select(a => a.CategoryId).ToListAsync(ct)).ToHashSet();

        var active = await db.FormAssignments.AsNoTracking()
            .Where(a => a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo >= now))
            .Select(a => new { a.FormId, a.AssignedToRole, a.AssignedToUserId, a.AssignedToStoreId, a.AssignedToCategoryId, a.ValidTo })
            .ToListAsync(ct);

        var formIds = new HashSet<Guid>();
        var validTo = new Dictionary<Guid, DateTimeOffset?>();
        foreach (var a in active)
        {
            var match = a.AssignedToRole == roleStr
                || a.AssignedToUserId == viewerId
                || (a.AssignedToStoreId is { } sid && storeIds.Contains(sid))
                || (a.AssignedToCategoryId is { } cid && categoryIds.Contains(cid));
            if (!match) continue;
            formIds.Add(a.FormId);
            // Keep the latest validTo seen (null = open-ended wins as "no deadline").
            if (!validTo.TryGetValue(a.FormId, out var cur) || cur is not null && (a.ValidTo is null || a.ValidTo > cur))
            {
                validTo[a.FormId] = a.ValidTo;
            }
        }

        return (formIds, validTo);
    }
}

// ----- Answer validation helpers -----

internal static class FormAnswers
{
    public static bool IsValidJson(string json)
    {
        try { using var _ = JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }

    /// <summary>
    /// Returns the id of the first required field missing a value, or null if all present.
    /// Section fields are skipped. (visible_if conditional skipping is deferred.)
    /// </summary>
    public static string? FindMissingRequired(string schema, string answers)
    {
        using var schemaDoc = JsonDocument.Parse(schema);
        using var ansDoc = JsonDocument.Parse(answers);
        if (!schemaDoc.RootElement.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var field in fields.EnumerateArray())
        {
            if (field.ValueKind != JsonValueKind.Object) continue;
            var type = field.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "section") continue;
            var required = field.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
            if (!required) continue;

            var id = field.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id)) continue;

            if (!ansDoc.RootElement.TryGetProperty(id, out var val) || IsEmpty(val))
            {
                return id;
            }
        }

        return null;
    }

    private static bool IsEmpty(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null => true,
        JsonValueKind.Undefined => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(v.GetString()),
        JsonValueKind.Array => v.GetArrayLength() == 0,
        _ => false,
    };
}
