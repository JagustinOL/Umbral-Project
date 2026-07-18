using MediatR;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Queries;

public sealed record GetAuditDashboardQuery(
    bool IsAdmin,
    Guid? OperatorId,
    int Limit) : IRequest<AuditDashboardDto>;

public sealed record GeneralRankingItemDto(
    int Position,
    Guid TeamId,
    string TeamName,
    int TotalScore,
    double ElapsedSeconds,
    Guid SessionId,
    Guid MissionId);

public sealed record MissionPlayCountDto(
    Guid MissionId,
    int SessionCount);

public sealed record OperatorSessionCountDto(
    Guid OperatorId,
    int SessionCount);

public sealed record AuditDashboardDto(
    IReadOnlyList<GeneralRankingItemDto> GeneralRanking,
    IReadOnlyList<MissionPlayCountDto> TopMissions,
    IReadOnlyList<OperatorSessionCountDto> TopOperators,
    int TotalFinishedSessions);

public sealed class GetAuditDashboardHandler
    : IRequestHandler<GetAuditDashboardQuery, AuditDashboardDto>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITeamLedgerRepository _teamLedgerRepository;

    public GetAuditDashboardHandler(
        IAuditLogRepository auditLogRepository,
        ITeamLedgerRepository teamLedgerRepository)
    {
        _auditLogRepository = auditLogRepository;
        _teamLedgerRepository = teamLedgerRepository;
    }

    public async Task<AuditDashboardDto> Handle(
        GetAuditDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);
        var operatorRef = request.IsAdmin ? null : request.OperatorId;
        if (!request.IsAdmin && operatorRef is null)
        {
            return new AuditDashboardDto([], [], [], 0);
        }

        var topScores = await _teamLedgerRepository.GetTopScoresAcrossFinishedSessionsAsync(
            operatorRef, limit, cancellationToken);
        var missionCounts = await _auditLogRepository.GetMissionPlayCountsAsync(
            operatorRef, limit, cancellationToken);
        var operatorCounts = await _auditLogRepository.GetOperatorSessionCountsAsync(
            operatorRef, limit, cancellationToken);
        var totalFinished = await _auditLogRepository.CountFinishedAsync(operatorRef, cancellationToken);

        var ranking = topScores
            .Select((item, index) => new GeneralRankingItemDto(
                index + 1,
                item.TeamId,
                item.TeamName,
                item.TotalScore,
                item.ElapsedSeconds,
                item.SessionId,
                item.MissionId))
            .ToList();

        var topMissions = missionCounts
            .Select(x => new MissionPlayCountDto(x.MissionId, x.SessionCount))
            .ToList();

        var topOperators = operatorCounts
            .Select(x => new OperatorSessionCountDto(x.OperatorId, x.SessionCount))
            .ToList();

        return new AuditDashboardDto(ranking, topMissions, topOperators, totalFinished);
    }
}
