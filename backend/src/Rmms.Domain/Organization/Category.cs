using Rmms.Domain.Common;

namespace Rmms.Domain.Organization;

/// <summary>
/// Product category / ngành hàng per M03 + <c>04-data-model.md</c> (categories).
/// Used later by the Form Engine (M10) and category-scoped assignments.
/// </summary>
public sealed class Category : AuditableEntity, IAggregateRoot
{
    /// <summary>Unique business code (e.g. <c>BEV</c>, <c>SNACK</c>).</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    private Category() { }

    public static Category Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            Code = code.Trim(),
            Name = name.Trim(),
        };
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
