using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints;

/// <summary>
/// Implementación real del acceso a pistas (sujeto del patrón Proxy).
/// </summary>
public sealed class MissionHintService : IHintAccessService
{
    private readonly IMissionRepository _repository;

    public MissionHintService(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<HintDto>> GetHintsForNodeAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var mission = await _repository.GetByIdAsync(missionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={missionId}.");

        var node = mission.FindNodeById(nodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={nodeId}.");

        if (node.NodeType == MissionNodeType.Stage)
            throw new InvalidOperationException(
                "Las pistas solo pueden consultarse en nodos de tipo 'Trivia' o 'TreasureHunt'.");

        return node.Hints
            .OrderBy(h => h.Order)
            .Select(h => new HintDto(h.Id, h.Order, h.Content, h.PenaltyPoints))
            .ToList();
    }
}
