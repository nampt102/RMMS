using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Organization;

/// <summary>
/// Product master row (M04 + <c>04-data-model.md</c> products). Foundation data for the
/// Form Engine (M10) product/SKU selectors (BR-507). Soft-deleted so historical form
/// submissions that reference a product keep resolving (M04 edge case).
/// </summary>
public sealed class Product : AuditableEntity, IAggregateRoot
{
    /// <summary>Unique stock-keeping unit (business key).</summary>
    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Brand { get; private set; }

    /// <summary>Optional FK to <c>categories</c> (M03). Null = uncategorised.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>Flexible ad-hoc attributes as a raw JSON string (jsonb column). Null = none.</summary>
    public string? Attributes { get; private set; }

    public ProductStatus Status { get; private set; }

    private Product() { } // EF Core

    public static Product Create(string sku, string name, string? brand, Guid? categoryId, string? attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Product
        {
            Sku = sku.Trim(),
            Name = name.Trim(),
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            CategoryId = categoryId,
            Attributes = string.IsNullOrWhiteSpace(attributes) ? null : attributes,
            Status = ProductStatus.Active,
        };
    }

    public void Update(string name, string? brand, Guid? categoryId, string? attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        CategoryId = categoryId;
        Attributes = string.IsNullOrWhiteSpace(attributes) ? null : attributes;
    }

    public void Activate() => Status = ProductStatus.Active;

    public void Deactivate() => Status = ProductStatus.Inactive;
}
