using FluentAssertions;
using Rmms.Domain.ValueObjects;
using Xunit;

namespace Rmms.UnitTests.Domain;

public sealed class GpsCoordinateTests
{
    [Fact]
    public void Construct_WithValidValues_Succeeds()
    {
        var gps = new GpsCoordinate(10.7769, 106.7009, accuracyMeters: 12.5, isMocked: false);

        gps.Latitude.Should().Be(10.7769);
        gps.Longitude.Should().Be(106.7009);
        gps.AccuracyMeters.Should().Be(12.5);
        gps.IsMocked.Should().BeFalse();
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Construct_WithOutOfRangeValues_Throws(double lat, double lng)
    {
        var act = () => new GpsCoordinate(lat, lng);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DistanceMetersTo_KnownPoints_ReturnsRoughlyCorrect()
    {
        // Saigon Notre-Dame Cathedral ↔ Bến Thành Market ≈ 700 m
        var a = new GpsCoordinate(10.7798, 106.6990);
        var b = new GpsCoordinate(10.7724, 106.6985);

        var d = a.DistanceMetersTo(b);

        d.Should().BeInRange(700, 900);
    }
}
