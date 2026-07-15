namespace SessionManagement.Application.Common.Interfaces;

public sealed record PlayerNodeContentData(
    Guid NodeId,
    string NodeType,
    IReadOnlyList<PlayerTriviaQuestionData>? Questions,
    string? Instructions,
    GpsCoordinateData? Destination
);

public sealed record PlayerTriviaQuestionData(
    string Prompt,
    IReadOnlyList<string> Options
);

public sealed record GpsCoordinateData(
    double Latitude,
    double Longitude
);
