namespace MissionManagement.Application.Common.Interfaces;

public interface IOperatorValidationService
{
    Task<bool> IsActiveOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default);
}
