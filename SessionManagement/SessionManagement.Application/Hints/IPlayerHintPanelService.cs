namespace SessionManagement.Application.Hints;

public interface IPlayerHintPanelService
{
    Task<IReadOnlyList<HintDto>> GetReleasedHintsForTeamAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        CancellationToken cancellationToken = default);
}

public sealed record HintDto(Guid Id, int Order, string Content, int PenaltyPoints);
