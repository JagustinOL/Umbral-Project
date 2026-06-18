using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Tests.Fakes;

public sealed class FakeIdentityService : IIdentityService
{
    public Task<CreateOperatorResult> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new CreateOperatorResult(Guid.NewGuid(), "ABCD-1234"));

    public Task SetupOperatorPasswordAsync(
        string email,
        string setupCode,
        string password,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<Guid> CreateAdminAsync(
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
