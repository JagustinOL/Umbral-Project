using Microsoft.EntityFrameworkCore;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using SessionManagement.Infrastructure.Persistence;

namespace SessionManagement.Infrastructure.Repositories;

public sealed class TeamRepository : ITeamRepository
{
    private readonly SessionManagementDbContext _dbContext;

    public TeamRepository(SessionManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .FirstOrDefaultAsync(x => x.Id == teamId && !x.IsDisbanded, cancellationToken);
    }

    public async Task<Team?> GetByCodeAsync(string teamCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamCode))
            throw new ArgumentException("TeamCode no puede estar vacío.", nameof(teamCode));

        var normalizedCode = teamCode.Trim().ToUpperInvariant();
        var normalizedTeamCode = TeamCode.From(normalizedCode);

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .FirstOrDefaultAsync(
                x => x.Code == normalizedTeamCode && !x.IsDisbanded,
                cancellationToken);
    }

    public async Task<Team?> GetActiveTeamByPlayerRefAsync(
        Guid playerRef,
        CancellationToken cancellationToken = default)
    {
        if (playerRef == Guid.Empty)
            throw new ArgumentException("PlayerRef no puede ser vacío.", nameof(playerRef));

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .FirstOrDefaultAsync(
                x => !x.IsDisbanded && x.Members.Any(m => m.PlayerRef == playerRef),
                cancellationToken);
    }

    public async Task<Team?> GetActiveTeamWithPendingJoinRequestAsync(
        Guid playerRef,
        CancellationToken cancellationToken = default)
    {
        if (playerRef == Guid.Empty)
            throw new ArgumentException("PlayerRef no puede ser vacío.", nameof(playerRef));

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .FirstOrDefaultAsync(
                x => !x.IsDisbanded &&
                     x.JoinRequests.Any(j =>
                         j.PlayerRef == playerRef &&
                         j.Status == JoinRequestStatus.Pending),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Team>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .Where(x => x.CurrentSessionRef == sessionId && !x.IsDisbanded)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Team>> GetByIdsAsync(
        IEnumerable<Guid> teamIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teamIds);

        var ids = teamIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _dbContext.Teams
            .Include(x => x.Members)
            .Include(x => x.JoinRequests)
            .Where(x => ids.Contains(x.Id) && !x.IsDisbanded)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string teamName,
        Guid? excludingTeamId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            throw new ArgumentException("TeamName no puede estar vacío.", nameof(teamName));

        var normalized = teamName.Trim().ToLowerInvariant();

        return await _dbContext.Teams
            .AsNoTracking()
            .Where(x => !x.IsDisbanded)
            .Where(x => excludingTeamId == null || x.Id != excludingTeamId.Value)
            .AnyAsync(x => x.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(
        string teamCode,
        Guid? excludingTeamId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamCode))
            throw new ArgumentException("TeamCode no puede estar vacío.", nameof(teamCode));

        var normalized = teamCode.Trim().ToUpperInvariant();
        var normalizedTeamCode = TeamCode.From(normalized);

        return await _dbContext.Teams
            .AsNoTracking()
            .Where(x => !x.IsDisbanded)
            .Where(x => excludingTeamId == null || x.Id != excludingTeamId.Value)
            .AnyAsync(x => x.Code == normalizedTeamCode, cancellationToken);
    }

    public async Task SaveAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        var exists = await _dbContext.Teams
            .AsNoTracking()
            .AnyAsync(x => x.Id == team.Id, cancellationToken);

        if (!exists)
            _dbContext.Teams.Add(team);
        else
            _dbContext.Teams.Update(team);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRangeAsync(IEnumerable<Team> teams, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teams);

        foreach (var team in teams)
            _dbContext.Teams.Update(team);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
