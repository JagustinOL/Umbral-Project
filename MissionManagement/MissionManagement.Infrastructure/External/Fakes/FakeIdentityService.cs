using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Infrastructure.External.Fakes;

public sealed class FakeIdentityService : IIdentityService
{
    public Task<Guid> CreateOperatorAsync(string firstName, string lastName, string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OperatorIdentityDto> result = [];
        return Task.FromResult(result);
    }

    public Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

