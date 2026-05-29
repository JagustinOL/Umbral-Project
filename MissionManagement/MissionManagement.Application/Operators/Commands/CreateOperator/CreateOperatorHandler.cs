using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Operators.Commands.CreateOperator;

public sealed class CreateOperatorHandler : IRequestHandler<CreateOperatorCommand, Guid>
{
    private readonly IIdentityService _identityService;

    public CreateOperatorHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Guid> Handle(CreateOperatorCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.CreateOperatorAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);
    }
}

