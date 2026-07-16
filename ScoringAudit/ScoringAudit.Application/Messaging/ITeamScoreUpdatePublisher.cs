using ScoringAudit.Application.Queries;

namespace ScoringAudit.Application.Messaging;

public interface ITeamScoreUpdatePublisher
{
    Task PublishAsync(TeamScoreUpdatedIntegrationEvent message, CancellationToken cancellationToken = default);
}

public static class TeamScoreUpdateRouting
{
    public const string RoutingKey = "scoring.team.score.updated";
}

public static class RankingSnapshotMapper
{
    public static IReadOnlyList<RankingItemDto> ToDto(
        IReadOnlyList<ScoringAudit.Domain.ValueObjects.RankingEntry> ranking) =>
        ranking
            .Select(r => new RankingItemDto(
                r.Position,
                r.TeamId,
                r.TeamName,
                r.TotalScore,
                r.CompletedNodes,
                r.TotalElapsedSeconds))
            .ToList();
}
