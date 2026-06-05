using Rmms.Domain.Common;
using Rmms.Domain.Enums;

namespace Rmms.Domain.Organization;

/// <summary>
/// Retail store (điểm bán) with GPS coordinates per M03 + <c>04-data-model.md</c> (stores).
///
/// IMPORTANT (domain vocabulary): a Store is a retail LOCATION with GPS coords — it is NOT a
/// time/shift container. GPS is required because check-in/out validates distance (BR-204).
/// </summary>
public sealed class Store : AuditableEntity, IAggregateRoot
{
    /// <summary>Unique business code (e.g. <c>ST-0001</c>).</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    /// <summary>WGS84 latitude, [-90, 90]. Required for BR-204 distance checks.</summary>
    public decimal Latitude { get; private set; }

    /// <summary>WGS84 longitude, [-180, 180]. Required for BR-204 distance checks.</summary>
    public decimal Longitude { get; private set; }

    public Guid? AreaId { get; private set; }

    public StoreStatus Status { get; private set; }

    private Store() { }

    public static Store Create(string code, string name, string? address, decimal latitude, decimal longitude, Guid? areaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateCoordinates(latitude, longitude);

        return new Store
        {
            Code = code.Trim(),
            Name = name.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            AreaId = areaId,
            Status = StoreStatus.Active,
        };
    }

    public void Update(string name, string? address, decimal latitude, decimal longitude, Guid? areaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateCoordinates(latitude, longitude);

        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        AreaId = areaId;
    }

    public void Activate() => Status = StoreStatus.Active;

    public void Deactivate() => Status = StoreStatus.Inactive;

    private static void ValidateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be within [-90, 90].");
        }
        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be within [-180, 180].");
        }
    }
}
