using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
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

    public async Task<IReadOnlyList<TeamReleasedHintDto>> GetAllReleasedHintsForTeamAsync(
        Guid sessionId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={sessionId}.");

        if (!session.RegisteredTeamIds.Contains(teamId))
            throw new NotFoundException($"El equipo {teamId} no pertenece a la sesión.");

        var released = session.ReleasedHints
            .Where(r => r.TeamId == teamId)
            .OrderBy(r => r.ReleasedAtUtc)
            .ToList();

        if (released.Count == 0)
            return [];

        var byNode = released.GroupBy(r => r.MissionNodeId);
        var catalogByHintId = new Dictionary<Guid, MissionHintData>();
        var contextByNode = new Dictionary<Guid, (string? NodeType, string? Prompt)>();

        foreach (var group in byNode)
        {
            var nodeHints = await _missionIntegration.GetHintsForNodeAsync(
                session.MissionRef, group.Key, cancellationToken);
            foreach (var hint in nodeHints)
                catalogByHintId[hint.Id] = hint;

            contextByNode[group.Key] = await MissionNodeContextHelper.GetContextAsync(
                _missionIntegration, session.MissionRef, group.Key, cancellationToken);
        }

        var result = new List<TeamReleasedHintDto>();
        foreach (var receipt in released)
        {
            if (!catalogByHintId.TryGetValue(receipt.HintId, out var catalog))
                continue;

            contextByNode.TryGetValue(receipt.MissionNodeId, out var context);

            result.Add(new TeamReleasedHintDto(
                HintId: receipt.HintId,
                MissionNodeId: receipt.MissionNodeId,
                Order: catalog.Order,
                Content: catalog.Content,
                PenaltyPoints: receipt.PenaltyPoints,
                ReleasedAtUtc: receipt.ReleasedAtUtc,
                WasManualRelease: receipt.WasManualRelease,
                NodeType: context.NodeType,
                NodePrompt: context.Prompt));
        }

        return result
            .OrderBy(h => h.ReleasedAtUtc)
            .ThenBy(h => h.Order)
            .ToList();
    }
}
