namespace MissionManagement.WebApi.Contracts.TreasureHunts;

public sealed record AddTreasureHuntNodeRequest(
    string Instructions,
    string SecretCode,
    GpsCoordinateRequest Destination,
    int ExecutionOrder,
    int BaseScore
);
