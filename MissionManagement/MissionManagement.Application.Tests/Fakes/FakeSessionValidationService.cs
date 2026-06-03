using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Tests.Fakes;

public sealed class FakeSessionValidationService : ISessionValidationService
{
    public bool HasActiveSessions { get; set; }
    public bool IsSupervisingMission { get; set; }
    public bool HasOpenSessionsForMission { get; set; }

    public Task<bool> HasActiveSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
        => Task.FromResult(HasActiveSessions);

    public Task<bool> IsSupervisingMissionAsync(Guid operatorId, Guid missionId, CancellationToken cancellationToken = default)
        => Task.FromResult(IsSupervisingMission);

    public Task<bool> HasOpenSessionsForMissionAsync(Guid missionId, CancellationToken cancellationToken = default)
        => Task.FromResult(HasOpenSessionsForMission);
}
