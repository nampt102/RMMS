using System.Text.Json;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Interfaces;
using Rmms.Domain.Common;
using Rmms.Domain.Enums;
using Rmms.Domain.Organization;
using Rmms.Shared.Errors;
using Rmms.Shared.Pagination;

namespace Rmms.Application.Organization.Products;

// ----- DTO -----

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Brand,
    Guid? CategoryId,
    string? CategoryName,
    string? Attributes,
    string Status,
    DateTimeOffset CreatedAt);

// ===== Create =====

public sealed record CreateProductCommand(string Sku, string Name, string? Brand, Guid? CategoryId, string? Attributes)
    : IRequest<Result<Guid>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.Brand).MaximumLength(255);
    }
}

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreateProductCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result<Guid>> Handle(CreateProductCommand command, CancellationToken ct)
    {
        if (!ProductRules.IsValidAttributes(command.Attributes))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.ValidationFailed, "Thuộc tính (attributes) phải là JSON hợp lệ."));
        }

        var sku = command.Sku.Trim();
        if (await _db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(ErrorCodes.CodeAlreadyExists, "Mã SKU đã tồn tại."));
        }

        if (command.CategoryId is { } catId && !await _db.Categories.AnyAsync(c => c.Id == catId, ct))
        {
            return Result.Failure<Guid>(Error.Validation(ErrorCodes.InvalidReference, "Ngành hàng không tồn tại."));
        }

        var product = Product.Create(sku, command.Name, command.Brand, command.CategoryId, command.Attributes);
        _db.Products.Add(product);

        await _audit.RecordAsync(AuditAction.ProductCreated, "product", product.Id, new { product.Sku, product.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(product.Id);
    }
}

// ===== Update =====

public sealed record UpdateProductCommand(Guid ProductId, string Name, string? Brand, Guid? CategoryId, string? Attributes)
    : IRequest<Result>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED").MaximumLength(255);
        RuleFor(x => x.Brand).MaximumLength(255);
    }
}

internal sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateProductCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(UpdateProductCommand command, CancellationToken ct)
    {
        if (!ProductRules.IsValidAttributes(command.Attributes))
        {
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Thuộc tính (attributes) phải là JSON hợp lệ."));
        }

        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy sản phẩm."));
        }

        if (command.CategoryId is { } catId && !await _db.Categories.AnyAsync(c => c.Id == catId, ct))
        {
            return Result.Failure(Error.Validation(ErrorCodes.InvalidReference, "Ngành hàng không tồn tại."));
        }

        product.Update(command.Name, command.Brand, command.CategoryId, command.Attributes);

        await _audit.RecordAsync(AuditAction.ProductUpdated, "product", product.Id, new { product.Name }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Change status =====

public sealed record ChangeProductStatusCommand(Guid ProductId, string Status) : IRequest<Result>;

internal sealed class ChangeProductStatusCommandHandler : IRequestHandler<ChangeProductStatusCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public ChangeProductStatusCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(ChangeProductStatusCommand command, CancellationToken ct)
    {
        if (!ProductRules.TryParseStatus(command.Status, out var status))
        {
            return Result.Failure(Error.Validation(ErrorCodes.ValidationFailed, "Trạng thái không hợp lệ (active/inactive)."));
        }

        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy sản phẩm."));
        }

        if (status == ProductStatus.Active) product.Activate();
        else product.Deactivate();

        await _audit.RecordAsync(AuditAction.ProductStatusChanged, "product", product.Id, new { Status = command.Status }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== Delete (soft) =====

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result>;

internal sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteProductCommandHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async ValueTask<Result> Handle(DeleteProductCommand command, CancellationToken ct)
    {
        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
        {
            return Result.Failure(Error.NotFound(ErrorCodes.NotFound, "Không tìm thấy sản phẩm."));
        }

        _db.Products.Remove(product);
        await _audit.RecordAsync(AuditAction.ProductDeleted, "product", product.Id, new { product.Sku }, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ===== List (admin full + mobile read-only) =====

/// <summary>
/// Paginated product list with search (name/sku/brand) + category filter (M04).
/// <paramref name="ActiveOnly"/> true → mobile read surface (active products only).
/// </summary>
public sealed record GetProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool ActiveOnly = false,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PaginatedResponse<ProductDto>>>;

internal sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PaginatedResponse<ProductDto>>>
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public GetProductsQueryHandler(IAppDbContext db) => _db = db;

    public async ValueTask<Result<PaginatedResponse<ProductDto>>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var q = _db.Products.AsNoTracking().AsQueryable();

        if (query.ActiveOnly) q = q.Where(p => p.Status == ProductStatus.Active);
        if (query.CategoryId is { } catId) q = q.Where(p => p.CategoryId == catId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => p.Sku.Contains(s) || p.Name.Contains(s) || (p.Brand != null && p.Brand.Contains(s)));
        }

        var total = await q.LongCountAsync(ct);
        var rows = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.Sku, p.Name, p.Brand, p.CategoryId, p.Attributes, p.Status, p.CreatedAt,
            })
            .ToListAsync(ct);

        // Resolve category names in one round-trip (mirrors AdminListAttendance store mapping).
        var catIds = rows.Where(r => r.CategoryId is not null).Select(r => r.CategoryId!.Value).Distinct().ToList();
        var catNames = catIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Categories.AsNoTracking()
                .Where(c => catIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var items = rows.Select(r => new ProductDto(
            r.Id, r.Sku, r.Name, r.Brand, r.CategoryId,
            r.CategoryId is { } id && catNames.TryGetValue(id, out var n) ? n : null,
            r.Attributes,
            r.Status == ProductStatus.Active ? "active" : "inactive",
            r.CreatedAt)).ToList();

        return Result.Success(new PaginatedResponse<ProductDto>(items, PaginationMeta.Build(page, pageSize, total)));
    }
}

// ----- Shared helpers -----

internal static class ProductRules
{
    public static bool IsValidAttributes(string? attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes)) return true;
        try { using var _ = JsonDocument.Parse(attributes); return true; }
        catch (JsonException) { return false; }
    }

    public static bool TryParseStatus(string raw, out ProductStatus status)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "active": status = ProductStatus.Active; return true;
            case "inactive": status = ProductStatus.Inactive; return true;
            default: status = default; return false;
        }
    }
}
