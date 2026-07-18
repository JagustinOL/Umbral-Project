namespace ScoringAudit.Domain.ReadModels;

public sealed record MissionPlayCount(Guid MissionId, int SessionCount);

public sealed record OperatorSessionCount(Guid OperatorId, int SessionCount);

public sealed record TopScoreAcrossSessions(
    Guid TeamId,
    string TeamName,
    int TotalScore,
    double ElapsedSeconds,
    Guid SessionId,
    Guid MissionId);
