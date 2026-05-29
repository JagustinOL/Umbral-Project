using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Infrastructure.External.Fakes;

public sealed class FakeSessionValidationService : ISessionValidationService
{
    public Task<bool> HasActiveSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsSupervisingMissionAsync(Guid operatorId, Guid missionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}

