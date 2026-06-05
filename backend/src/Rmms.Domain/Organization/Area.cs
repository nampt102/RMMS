using Rmms.Domain.Common;

namespace Rmms.Domain.Organization;

/// <summary>
/// Geographic / organizational area (khu vực) per M03 + <c>04-data-model.md</c> (areas).
/// Supports optional self-referencing hierarchy via <see cref="ParentAreaId"/>.
/// </summary>
public sealed class Area : AuditableEntity, IAggregateRoot
{
    /// <summary>Unique business code (e.g. <c>HCM</c>, <c>HN-Q1</c>).</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Parent area for hierarchy; null = top-level.</summary>
    public Guid? ParentAreaId { get; private set; }

    private Area() { }

    public static Area Create(string code, string name, Guid? parentAreaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Area
        {
            Code = code.Trim(),
            Name = name.Trim(),
            ParentAreaId = parentAreaId,
        };
    }

    public void Update(string name, Guid? parentAreaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (parentAreaId == Id)
        {
            throw new InvalidOperationException("An area cannot be its own parent.");
        }

        Name = name.Trim();
        ParentAreaId = parentAreaId;
    }
}
