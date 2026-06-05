namespace MissionManagement.WebApi.Contracts.TreasureHunts;

public sealed record UpdateTreasureHuntNodeRequest(
    string Instructions,
    string SecretCode,
    GpsCoordinateRequest Destination
);

