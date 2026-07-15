using Microsoft.EntityFrameworkCore;
using ScoringAudit.Domain.Aggregates;
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

    public async Task SaveAsync(TeamLedger ledger, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(ledger);
        if (entry.State == EntityState.Detached)
            _dbContext.TeamLedgers.Add(ledger);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
