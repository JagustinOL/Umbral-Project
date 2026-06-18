using MissionManagement.Application.Common;

namespace MissionManagement.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<CreateOperatorResult> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);

    Task SetupOperatorPasswordAsync(
        string email,
        string setupCode,
        string password,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAdminAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default);

    Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default);
}
