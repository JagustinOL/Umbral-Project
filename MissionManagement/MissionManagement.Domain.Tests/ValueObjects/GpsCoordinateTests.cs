using FluentAssertions;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.ValueObjects;

public sealed class GpsCoordinateTests
{
    [Fact]
    public void Constructor_WhenLatitudeIsOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const double latitude = 91;
        const double longitude = -74.1234;

        // Act
        var action = () => new GpsCoordinate(latitude, longitude);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*latitud*");
    }

    [Fact]
    public void Constructor_WhenLongitudeIsOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const double latitude = 4.711;
        const double longitude = -181;

        // Act
        var action = () => new GpsCoordinate(latitude, longitude);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*longitud*");
    }

    [Fact]
    public void Constructor_WhenValuesAreValid_CreatesGpsCoordinate()
    {
        // Arrange
        const double latitude = 4.711;
        const double longitude = -74.0721;

        // Act
        var result = new GpsCoordinate(latitude, longitude);

        // Assert
        result.Latitude.Should().Be(latitude);
        result.Longitude.Should().Be(longitude);
    }
}

