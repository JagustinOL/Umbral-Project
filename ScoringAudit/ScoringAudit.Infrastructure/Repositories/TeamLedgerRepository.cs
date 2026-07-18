using Microsoft.EntityFrameworkCore;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.ReadModels;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Infrastructure.Persistence;

namespace ScoringAudit.Infrastructure.Repositories;

public sealed class TeamLedgerRepository : ITeamLedgerRepository
{
    private readonly ScoringAuditDbContext _dbContext;

    public TeamLedgerRepository(ScoringAuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeamLedger?> GetByTeamAndSessionAsync(
        Guid teamRef,
        Guid sessionRef,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamLedgers
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.TeamRef == teamRef && x.SessionRef == sessionRef, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamLedger>> GetBySessionAsync(
        Guid sessionRef,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamLedgers
            .Include(x => x.Entries)
            .Where(x => x.SessionRef == sessionRef)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopScoreAcrossSessions>> GetTopScoresAcrossFinishedSessionsAsync(
        Guid? operatorRef,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var finishedAudits = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.IsClosed && a.Status == "Finished");

        if (operatorRef.HasValue)
            finishedAudits = finishedAudits.Where(a => a.OperatorRef == operatorRef.Value);

        var sessionMissions = await finishedAudits
            .Select(a => new { a.SessionRef, a.MissionRef })
            .ToListAsync(cancellationToken);

        if (sessionMissions.Count == 0)
            return [];

        var missionBySession = sessionMissions
            .GroupBy(x => x.SessionRef)
            .ToDictionary(g => g.Key, g => g.First().MissionRef);
        var sessionIds = missionBySession.Keys.ToList();

        var ledgers = await _dbContext.TeamLedgers
            .AsNoTracking()
            .Include(x => x.Entries)
            .Where(x => sessionIds.Contains(x.SessionRef))
            .ToListAsync(cancellationToken);

        return ledgers
            .Select(ledger => new TopScoreAcrossSessions(
                ledger.TeamRef,
                ledger.TeamName,
                ledger.TotalScore,
                ledger.TotalElapsedSeconds,
                ledger.SessionRef,
                missionBySession[ledger.SessionRef]))
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.ElapsedSeconds)
            .Take(limit)
            .ToList();
    }

    public async Task SaveAsync(TeamLedger ledger, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(ledger);
        if (entry.State == EntityState.Detached)
            _dbContext.TeamLedgers.Add(ledger);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
