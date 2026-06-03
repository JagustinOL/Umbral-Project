namespace MissionManagement.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Guid> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default);
    Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default);
}

