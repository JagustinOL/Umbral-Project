using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Hints;

/// <summary>
/// Patrón Proxy — controla acceso a pistas según rol y estado de la misión.
/// Admin/operador siempre pueden consultar; otros roles solo en borrador.
/// </summary>
public sealed class DraftOnlyHintProxy : IHintAccessService
{
    private readonly MissionHintService _inner;
    private readonly IMissionRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DraftOnlyHintProxy(
        MissionHintService inner,
        IMissionRepository repository,
        ICurrentUser currentUser)
    {
        _inner = inner;
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<HintDto>> GetHintsForNodeAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsInRole("admin") || _currentUser.IsInRole("operator"))
            return await _inner.GetHintsForNodeAsync(missionId, nodeId, cancellationToken);

        // Llamadas service-to-service (p. ej. SessionManagement → operator-board / proxy jugador)
        // llegan sin principal autenticado. El filtrado de pistas liberadas ocurre en SessionManagement.
        if (!_currentUser.IsAuthenticated)
            return await _inner.GetHintsForNodeAsync(missionId, nodeId, cancellationToken);

        var mission = await _repository.GetByIdAsync(missionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={missionId}.");

        if (mission.Status != MissionStatus.Draft)
            throw new UnauthorizedException(
                "Las pistas solo pueden consultarse en detalle cuando la misión está en borrador.");

        return await _inner.GetHintsForNodeAsync(missionId, nodeId, cancellationToken);
    }
}
