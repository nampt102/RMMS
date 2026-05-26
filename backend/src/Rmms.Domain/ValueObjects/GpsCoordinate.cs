using Rmms.Domain.Common;

namespace Rmms.Domain.ValueObjects;

/// <summary>
/// GPS coordinate value object used in check-in/check-out flows (BR-201..BR-210).
/// Validation enforced by FluentValidation at application boundary;
/// the constructor guards against obviously-invalid values to keep domain consistent.
/// </summary>
public sealed class GpsCoordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double? AccuracyMeters { get; }
    public bool IsMocked { get; }

    public GpsCoordinate(double latitude, double longitude, double? accuracyMeters = null, bool isMocked = false)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be in [-90, 90].");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be in [-180, 180].");
        if (accuracyMeters is < 0)
            throw new ArgumentOutOfRangeException(nameof(accuracyMeters), "Accuracy must be non-negative.");

        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        IsMocked = isMocked;
    }

    /// <summary>
    /// Haversine distance in meters between two GPS points.
    /// Used by check-in geofencing per BR-204.
    /// </summary>
    public double DistanceMetersTo(GpsCoordinate other)
    {
        const double earthRadiusM = 6_371_000;
        var dLat = ToRad(other.Latitude - Latitude);
        var dLon = ToRad(other.Longitude - Longitude);
        var lat1 = ToRad(Latitude);
        var lat2 = ToRad(other.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusM * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return AccuracyMeters;
        yield return IsMocked;
    }
}
