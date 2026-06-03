using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Common;

public static class TeamSessionLockService
{
    public static async Task LockTeamsForSessionAsync(
        LiveSession session,
        ITeamRepository teamRepository,
        CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetByIdsAsync(session.RegisteredTeamIds, cancellationToken);
        foreach (var team in teams)
            team.Lock();

        if (teams.Count > 0)
            await teamRepository.SaveRangeAsync(teams, cancellationToken);
    }

    public static async Task ReleaseTeamsFromSessionAsync(
        LiveSession session,
        ITeamRepository teamRepository,
        CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetByIdsAsync(session.RegisteredTeamIds, cancellationToken);
        foreach (var team in teams)
            team.ReleaseFromSession();

        if (teams.Count > 0)
            await teamRepository.SaveRangeAsync(teams, cancellationToken);
    }

    public static async Task AssignTeamToSessionAsync(
        Guid teamId,
        Guid sessionId,
        ITeamRepository teamRepository,
        CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new Exceptions.NotFoundException($"No se encontró el equipo con Id={teamId}.");

        try
        {
            team.AssignToSession(sessionId);
        }
        catch (SessionDomainException ex)
        {
            throw new Exceptions.ConflictException(ex.Message);
        }

        await teamRepository.SaveAsync(team, cancellationToken);
    }
}
