using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Hints;

/// <summary>
/// Patrón Proxy — expone al jugador solo las pistas liberadas por el operador.
/// </summary>
public sealed class PlayerReleasedHintsProxy : IPlayerHintPanelService
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IMissionIntegrationService _missionIntegration;

    public PlayerReleasedHintsProxy(
        ILiveSessionRepository sessionRepository,
        IMissionIntegrationService missionIntegration)
    {
        _sessionRepository = sessionRepository;
        _missionIntegration = missionIntegration;
    }

    public async Task<IReadOnlyList<HintDto>> GetReleasedHintsForTeamAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={sessionId}.");

        if (!session.RegisteredTeamIds.Contains(teamId))
            throw new NotFoundException($"El equipo {teamId} no pertenece a la sesión.");

        var releasedHintIds = session.ReleasedHints
            .Where(r => r.TeamId == teamId && r.MissionNodeId == nodeId)
            .Select(r => r.HintId)
            .ToHashSet();

        var allHints = await _missionIntegration.GetHintsForNodeAsync(
            session.MissionRef,
            nodeId,
            cancellationToken);

        return allHints
            .Where(h => releasedHintIds.Contains(h.Id))
            .Select(h => new HintDto(h.Id, h.Order, h.Content, h.PenaltyPoints))
            .OrderBy(h => h.Order)
            .ToList();
    }
}
