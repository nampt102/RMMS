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

// ----- DTOs -----

public sealed record FormSummaryDto(
    Guid Id, string Code, string NameVi, string NameEn, string FormType, string Status,
    int CurrentVersion, bool HasDraft, DateTimeOffset CreatedAt);

public sealed record FormDetailDto(
    Guid Id, string Code, string NameVi, string NameEn, string? DescriptionVi, string? DescriptionEn,
    string FormType, string Status, int CurrentVersion, int EditableVersion, bool HasDraft, string Schema);

public sealed record FormVersionDto(int Version, bool IsPublished, DateTimeOffset? PublishedAt, DateTimeOffset CreatedAt);

// ===== Create =====

public sealed record CreateFormCommand(
    string Code, string NameVi, string NameEn, string? DescriptionVi, string? DescriptionEn,
    string FormType, string Schema) : IRequest<Result<Guid>>;

public sealed class CreateFormCommandValidator : AbstractValidator<CreateFormCommand>
{
    public CreateFormCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(50);
        RuleFor(x => x.NameVi).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.NameEn).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.FormType).NotEmpty().WithErrorCode("REQUIRED");
    }
}

internal sealed class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateFormCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateFormCommand command, CancellationToken ct)
    {
        if (!FormTypeMap.TryParse(command.FormType, out var formType))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Loại form không hợp lệ."));
        }

        var schemaCheck = FormSchema.Validate(command.Schema);
        if (!schemaCheck.IsValid)
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, schemaCheck.Error!));
        }

        var code = command.Code.Trim();
        if (await _db.Forms.AnyAsync(f => f.Code == code, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(ErrorCodes.CodeAlreadyExists, "Mã form đã tồn tại."));
        }

        var form = Form.Create(code, command.NameVi, command.NameEn, command.DescriptionVi, command.DescriptionEn, formType);
        _db.Forms.Add(form);
        _db.FormVersions.Add(FormVersion.CreateDraft(form.Id, 1, command.Schema));

        await _audit.RecordAsync(AuditAction.FormCreated, "form", form.Id, new { form.Code, Type = command.FormType }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(form.Id);
    }
}

// ===== Update draft (BR-505: edit published → new draft version) =====

public sealed record UpdateFormDraftCommand(
    Guid FormId, string NameVi, string NameEn, string? DescriptionVi, string? DescriptionEn, string Schema)
    : IRequest<Result>;

public sealed class UpdateFormDraftCommandValidator : AbstractValidator<UpdateFormDraftCommand>
{
    public UpdateFormDraftCommandValidator()
    {
        RuleFor(x => x.NameVi).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.NameEn).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
    }
}

internal sealed class UpdateFormDraftCommandHandler : IRequestHandler<UpdateFormDraftCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateFormDraftCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateFormDraftCommand command, CancellationToken ct)
    {
        var schemaCheck = FormSchema.Validate(command.Schema);
        if (!schemaCheck.IsValid)
        {
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, schemaCheck.Error!));
        }

        var form = await _db.Forms.SingleOrDefaultAsync(f => f.Id == command.FormId, ct);
        if (form is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }

        form.UpdateMeta(command.NameVi, command.NameEn, command.DescriptionVi, command.DescriptionEn);

        var versions = await _db.FormVersions.Where(v => v.FormId == form.Id).ToListAsync(ct);
        var draft = versions.FirstOrDefault(v => !v.IsPublished);
        if (draft is not null)
        {
            draft.UpdateSchema(command.Schema);
        }
        else
        {
            var nextVersion = versions.Count == 0 ? 1 : versions.Max(v => v.Version) + 1;
            _db.FormVersions.Add(FormVersion.CreateDraft(form.Id, nextVersion, command.Schema));
        }

        await _audit.RecordAsync(AuditAction.FormUpdated, "form", form.Id, new { form.Code }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Publish =====

public sealed record PublishFormCommand(Guid FormId) : IRequest<Result<int>>;

internal sealed class PublishFormCommandHandler : IRequestHandler<PublishFormCommand, Result<int>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public PublishFormCommandHandler(IAppDbContext db, IAuditLogger audit, IDateTimeProvider clock, ICurrentUser currentUser)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<Result<int>> Handle(PublishFormCommand command, CancellationToken ct)
    {
        var form = await _db.Forms.SingleOrDefaultAsync(f => f.Id == command.FormId, ct);
        if (form is null)
        {
            return Result.Failure<int>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }

        var draft = await _db.FormVersions
            .Where(v => v.FormId == form.Id && v.PublishedAt == null)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(ct);
        if (draft is null)
        {
            return Result.Failure<int>(Error.Conflict(ErrorCodes.Conflict, "Không có bản nháp nào để publish."));
        }

        var schemaCheck = FormSchema.Validate(draft.Schema);
        if (!schemaCheck.IsValid)
        {
            return Result.Failure<int>(Error.Validation(ErrorCodes.ValidationFailed, schemaCheck.Error!));
        }

        draft.Publish(_clock.UtcNow, _currentUser.UserId);
        form.MarkPublished(draft.Version);

        await _audit.RecordAsync(AuditAction.FormPublished, "form", form.Id, new { form.Code, Version = draft.Version }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(draft.Version);
    }
}

// ===== List =====

public sealed record GetFormsQuery : IRequest<Result<IReadOnlyList<FormSummaryDto>>>;

internal sealed class GetFormsQueryHandler : IRequestHandler<GetFormsQuery, Result<IReadOnlyList<FormSummaryDto>>>
{
    private readonly IAppDbContext _db;

    public GetFormsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<FormSummaryDto>>> Handle(GetFormsQuery query, CancellationToken ct)
    {
        var forms = await _db.Forms.AsNoTracking().OrderBy(f => f.Code)
            .Select(f => new { f.Id, f.Code, f.NameVi, f.NameEn, f.FormType, f.Status, f.CurrentVersion, f.CreatedAt })
            .ToListAsync(ct);

        // Forms that have an unpublished (draft) version awaiting publish.
        var draftFormIds = (await _db.FormVersions.AsNoTracking()
            .Where(v => v.PublishedAt == null)
            .Select(v => v.FormId)
            .ToListAsync(ct)).ToHashSet();

        var items = forms.Select(f => new FormSummaryDto(
            f.Id, f.Code, f.NameVi, f.NameEn, FormTypeMap.ToText(f.FormType), FormStatusText(f.Status),
            f.CurrentVersion, draftFormIds.Contains(f.Id), f.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<FormSummaryDto>>(items);
    }

    private static string FormStatusText(FormStatus s) => s switch
    {
        FormStatus.Draft => "draft",
        FormStatus.Published => "published",
        FormStatus.Archived => "archived",
        _ => "draft",
    };
}

// ===== Detail (latest editable/published schema) =====

public sealed record GetFormQuery(Guid FormId) : IRequest<Result<FormDetailDto>>;

internal sealed class GetFormQueryHandler : IRequestHandler<GetFormQuery, Result<FormDetailDto>>
{
    private readonly IAppDbContext _db;

    public GetFormQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<FormDetailDto>> Handle(GetFormQuery query, CancellationToken ct)
    {
        var form = await _db.Forms.AsNoTracking().SingleOrDefaultAsync(f => f.Id == query.FormId, ct);
        if (form is null)
        {
            return Result.Failure<FormDetailDto>(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy form."));
        }

        var versions = await _db.FormVersions.AsNoTracking()
            .Where(v => v.FormId == form.Id)
            .OrderByDescending(v => v.Version)
            .Select(v => new { v.Version, v.Schema, v.PublishedAt })
            .ToListAsync(ct);

        // The latest version is what the Builder edits (draft if present, else the highest published).
        var latest = versions.FirstOrDefault();
        var hasDraft = latest is not null && latest.PublishedAt == null;

        return Result.Success(new FormDetailDto(
            form.Id, form.Code, form.NameVi, form.NameEn, form.DescriptionVi, form.DescriptionEn,
            FormTypeMap.ToText(form.FormType),
            form.Status switch { FormStatus.Published => "published", FormStatus.Archived => "archived", _ => "draft" },
            form.CurrentVersion,
            latest?.Version ?? 0,
            hasDraft,
            latest?.Schema ?? "{\"fields\":[]}"));
    }
}

// ===== Version history =====

public sealed record GetFormVersionsQuery(Guid FormId) : IRequest<Result<IReadOnlyList<FormVersionDto>>>;

internal sealed class GetFormVersionsQueryHandler
    : IRequestHandler<GetFormVersionsQuery, Result<IReadOnlyList<FormVersionDto>>>
{
    private readonly IAppDbContext _db;

    public GetFormVersionsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<FormVersionDto>>> Handle(GetFormVersionsQuery query, CancellationToken ct)
    {
        var rows = await _db.FormVersions.AsNoTracking()
            .Where(v => v.FormId == query.FormId)
            .OrderByDescending(v => v.Version)
            .Select(v => new FormVersionDto(v.Version, v.PublishedAt != null, v.PublishedAt, v.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<FormVersionDto>>(rows);
    }
}

// ----- Shared form_type <-> snake_case mapping -----

internal static class FormTypeMap
{
    public static bool TryParse(string raw, out FormType type)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "stock_report": type = FormType.StockReport; return true;
            case "market_report": type = FormType.MarketReport; return true;
            case "photo_report": type = FormType.PhotoReport; return true;
            case "pc_checklist": type = FormType.PcChecklist; return true;
            case "free_report": type = FormType.FreeReport; return true;
            case "survey": type = FormType.Survey; return true;
            case "knowledge_test": type = FormType.KnowledgeTest; return true;
            case "training": type = FormType.Training; return true;
            case "visit_report": type = FormType.VisitReport; return true;
            default: type = default; return false;
        }
    }

    public static string ToText(FormType type) => type switch
    {
        FormType.StockReport => "stock_report",
        FormType.MarketReport => "market_report",
        FormType.PhotoReport => "photo_report",
        FormType.PcChecklist => "pc_checklist",
        FormType.FreeReport => "free_report",
        FormType.Survey => "survey",
        FormType.KnowledgeTest => "knowledge_test",
        FormType.Training => "training",
        FormType.VisitReport => "visit_report",
        _ => "free_report",
    };
}
