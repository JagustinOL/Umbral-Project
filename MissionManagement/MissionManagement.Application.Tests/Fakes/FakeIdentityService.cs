using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Tests.Fakes;

public sealed class FakeIdentityService : IIdentityService
{
    public Task<Guid> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid());

    public Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OperatorIdentityDto>>([]);

    public Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
