using MediatR;
using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Operators.Commands.CreateOperator;

public sealed class CreateOperatorHandler : IRequestHandler<CreateOperatorCommand, CreateOperatorResult>
{
    private readonly IIdentityService _identityService;

    public CreateOperatorHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<CreateOperatorResult> Handle(CreateOperatorCommand request, CancellationToken cancellationToken) =>
        _identityService.CreateOperatorAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);
}
