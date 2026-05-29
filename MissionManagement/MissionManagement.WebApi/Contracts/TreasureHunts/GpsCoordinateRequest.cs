namespace MissionManagement.WebApi.Contracts.TreasureHunts;

public sealed record GpsCoordinateRequest(
    double Latitude,
    double Longitude
);

