using Microsoft.EntityFrameworkCore;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using MissionManagement.Infrastructure.Persistence;

namespace MissionManagement.Infrastructure.Repositories;

public sealed class MissionRepository : IMissionRepository
{
    private readonly MissionManagementDbContext _dbContext;

    public MissionRepository(MissionManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Mission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Missions
            .AsNoTracking()
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Mission?> GetByIdAsync(Guid missionId, CancellationToken cancellationToken = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("El missionId no puede ser vacío.", nameof(missionId));

        return await _dbContext.Missions
            .AsNoTracking()
            .Include(m => m.Nodes)
                .ThenInclude(n => n.Hints)
            .Include(m => m.Nodes)
                .ThenInclude(n => n.Children)
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
    }

    public async Task<bool> TitleExistsAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título no puede estar vacío.", nameof(title));

        var normalized = title.Trim();

        return await _dbContext.Missions
            .AsNoTracking()
            .AnyAsync(m => m.Title.ToLower() == normalized.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Mission>> GetActiveMissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Missions
            .AsNoTracking()
            .Where(m => m.Status == MissionStatus.Active)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(Mission mission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mission);

        var exists = await _dbContext.Missions
            .AsNoTracking()
            .AnyAsync(m => m.Id == mission.Id, cancellationToken);

        if (!exists)
        {
            _dbContext.Missions.Add(mission);
        }
        else
        {
            _dbContext.Missions.Update(mission);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

