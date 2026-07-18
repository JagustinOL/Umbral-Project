using Microsoft.EntityFrameworkCore;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.ReadModels;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Infrastructure.Persistence;

namespace ScoringAudit.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ScoringAuditDbContext _dbContext;

    public AuditLogRepository(ScoringAuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AuditLog?> GetBySessionAsync(Guid sessionRef, CancellationToken cancellationToken = default) =>
        _dbContext.AuditLogs
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.SessionRef == sessionRef, cancellationToken);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetClosedPaginatedAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? operatorRef,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs
            .Where(x => x.IsClosed)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(x => x.EndedAtUtc >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(x => x.EndedAtUtc <= endDate.Value);
        if (operatorRef.HasValue)
            query = query.Where(x => x.OperatorRef == operatorRef.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.EndedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountFinishedAsync(
        Guid? operatorRef,
        CancellationToken cancellationToken = default)
    {
        return await FinishedQuery(operatorRef).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionPlayCount>> GetMissionPlayCountsAsync(
        Guid? operatorRef,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await FinishedQuery(operatorRef)
            .GroupBy(x => x.MissionRef)
            .Select(g => new { MissionId = g.Key, SessionCount = g.Count() })
            .OrderByDescending(x => x.SessionCount)
            .ThenBy(x => x.MissionId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new MissionPlayCount(x.MissionId, x.SessionCount))
            .ToList();
    }

    public async Task<IReadOnlyList<OperatorSessionCount>> GetOperatorSessionCountsAsync(
        Guid? operatorRef,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await FinishedQuery(operatorRef)
            .GroupBy(x => x.OperatorRef)
            .Select(g => new { OperatorId = g.Key, SessionCount = g.Count() })
            .OrderByDescending(x => x.SessionCount)
            .ThenBy(x => x.OperatorId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new OperatorSessionCount(x.OperatorId, x.SessionCount))
            .ToList();
    }

    public async Task SaveAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(auditLog).State == EntityState.Detached)
            _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AuditLog> FinishedQuery(Guid? operatorRef)
    {
        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.IsClosed && x.Status == "Finished");

        if (operatorRef.HasValue)
            query = query.Where(x => x.OperatorRef == operatorRef.Value);

        return query;
    }
}
