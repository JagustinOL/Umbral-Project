namespace MissionManagement.Domain.ValueObjects;

public sealed record GpsCoordinate
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public GpsCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude),
                "La latitud debe estar en el rango [-90, 90].");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude),
                "La longitud debe estar en el rango [-180, 180].");

        Latitude = latitude;
        Longitude = longitude;
    }
}
