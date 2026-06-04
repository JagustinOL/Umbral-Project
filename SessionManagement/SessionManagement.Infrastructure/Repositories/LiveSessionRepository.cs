using Microsoft.EntityFrameworkCore;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Infrastructure.Persistence;

namespace SessionManagement.Infrastructure.Repositories;

public sealed class LiveSessionRepository : ILiveSessionRepository
{
    private readonly SessionManagementDbContext _dbContext;

    public LiveSessionRepository(SessionManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LiveSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));

        return await _dbContext.LiveSessions
            .Include(x => x.EvidenceSubmissions)
            .Include(x => x.ReleasedHints)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<LiveSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
            throw new ArgumentException("JoinCode no puede estar vacío.", nameof(joinCode));

        var normalized = joinCode.Trim().ToUpperInvariant();

        return await _dbContext.LiveSessions
            .Include(x => x.EvidenceSubmissions)
            .Include(x => x.ReleasedHints)
            .FirstOrDefaultAsync(x => x.JoinCode == normalized, cancellationToken);
    }

    public async Task<LiveSession?> GetByIdForOperatorAsync(
        Guid sessionId,
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));

        return await _dbContext.LiveSessions
            .Include(x => x.EvidenceSubmissions)
            .Include(x => x.ReleasedHints)
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.OperatorRef == operatorId, cancellationToken);
    }

    public async Task<IReadOnlyList<LiveSession>> GetPendingByOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));

        return await _dbContext.LiveSessions
            .AsNoTracking()
            .Where(x => x.OperatorRef == operatorId && x.Status == LiveSessionStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LiveSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        // Sesiones visibles para jugadores: abiertas a registro (Pending/Preparation) y en curso.
        return await _dbContext.LiveSessions
            .AsNoTracking()
            .Where(x =>
                x.Status == LiveSessionStatus.Pending
                || x.Status == LiveSessionStatus.Preparation
                || x.Status == LiveSessionStatus.Active
                || x.Status == LiveSessionStatus.Paused)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenSessionsByOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));

        return await _dbContext.LiveSessions
            .AsNoTracking()
            .AnyAsync(
                x => x.OperatorRef == operatorId
                     && (x.Status == LiveSessionStatus.Pending
                         || x.Status == LiveSessionStatus.Preparation
                         || x.Status == LiveSessionStatus.Active
                         || x.Status == LiveSessionStatus.Paused),
                cancellationToken);
    }

    public async Task<bool> HasOpenSessionForMissionByOperatorAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId no puede ser vacío.", nameof(missionId));

        return await _dbContext.LiveSessions
            .AsNoTracking()
            .AnyAsync(
                x => x.OperatorRef == operatorId
                     && x.MissionRef == missionId
                     && (x.Status == LiveSessionStatus.Pending
                         || x.Status == LiveSessionStatus.Preparation
                         || x.Status == LiveSessionStatus.Active
                         || x.Status == LiveSessionStatus.Paused),
                cancellationToken);
    }

    public async Task<bool> HasOpenSessionsByMissionAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId no puede ser vacío.", nameof(missionId));

        return await _dbContext.LiveSessions
            .AsNoTracking()
            .AnyAsync(
                x => x.MissionRef == missionId
                     && (x.Status == LiveSessionStatus.Pending
                         || x.Status == LiveSessionStatus.Preparation
                         || x.Status == LiveSessionStatus.Active
                         || x.Status == LiveSessionStatus.Paused),
                cancellationToken);
    }

    public async Task SaveAsync(LiveSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var exists = await _dbContext.LiveSessions
            .AsNoTracking()
            .AnyAsync(x => x.Id == session.Id, cancellationToken);

        if (!exists)
            _dbContext.LiveSessions.Add(session);
        else
            _dbContext.LiveSessions.Update(session);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

