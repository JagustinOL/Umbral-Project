using MissionManagement.Application.Dtos;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints;

public interface IHintAccessService
{
    Task<IReadOnlyList<HintDto>> GetHintsForNodeAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default);
}
