namespace UserService.Application.Common.Interfaces;

public interface ISessionValidationService
{
    Task<bool> HasActiveSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<bool> IsSupervisingMissionAsync(Guid operatorId, Guid missionId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenSessionsForMissionAsync(Guid missionId, CancellationToken cancellationToken = default);
}

