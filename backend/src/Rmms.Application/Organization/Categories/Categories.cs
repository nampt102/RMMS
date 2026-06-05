using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;

namespace Rmms.Application.Organization.Categories;

// ----- DTO -----

public sealed record CategoryDto(Guid Id, string Code, string Name, DateTimeOffset CreatedAt);

// ===== Create =====

public sealed record CreateCategoryCommand(string Code, string Name) : IRequest<Result<Guid>>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
    }
}

internal sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateCategoryCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();
        if (await _db.Categories.AnyAsync(c => c.Code == code, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(ErrorCodes.CodeAlreadyExists, "Mã ngành hàng đã tồn tại."));
        }

        var category = Category.Create(code, command.Name);
        _db.Categories.Add(category);

        await _audit.RecordAsync(AuditAction.CategoryCreated, "category", category.Id, new { category.Code, category.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(category.Id);
    }
}

// ===== Update =====

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name) : IRequest<Result>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
}

internal sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateCategoryCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateCategoryCommand command, CancellationToken ct)
    {
        var category = await _db.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, ct);
        if (category is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy ngành hàng."));
        }

        category.Update(command.Name);

        await _audit.RecordAsync(AuditAction.CategoryUpdated, "category", category.Id, new { category.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Delete (soft) =====

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest<Result>;

internal sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteCategoryCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteCategoryCommand command, CancellationToken ct)
    {
        var category = await _db.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, ct);
        if (category is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy ngành hàng."));
        }

        _db.Categories.Remove(category);
        await _audit.RecordAsync(AuditAction.CategoryDeleted, "category", category.Id, new { category.Code }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== List =====

public sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

internal sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IAppDbContext _db;

    public GetCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery query, CancellationToken ct)
    {
        var rows = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CategoryDto(c.Id, c.Code, c.Name, c.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<CategoryDto>>(rows);
    }
}
