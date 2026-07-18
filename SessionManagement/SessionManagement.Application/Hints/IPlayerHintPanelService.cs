using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.Hints;

public interface IPlayerHintPanelService
{
    Task<IReadOnlyList<HintDto>> GetReleasedHintsForTeamAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamReleasedHintDto>> GetAllReleasedHintsForTeamAsync(
        Guid sessionId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}

public sealed record HintDto(Guid Id, int Order, string Content, int PenaltyPoints);
