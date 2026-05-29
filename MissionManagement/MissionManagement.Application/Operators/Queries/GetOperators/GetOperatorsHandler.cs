using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Operators.Queries.GetOperators;

public sealed class GetOperatorsHandler : IRequestHandler<GetOperatorsQuery, IReadOnlyList<OperatorIdentityDto>>
{
    private readonly IIdentityService _identityService;

    public GetOperatorsHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<OperatorIdentityDto>> Handle(GetOperatorsQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetOperatorsAsync(cancellationToken);
    }
}

