namespace Rmms.Application.Organization.Stores;

/// <summary>Store list/detail projection for Admin web. Mirrors <c>04-data-model.md</c> (stores).</summary>
public sealed record StoreDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    Guid? AreaId,
    string? AreaName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
