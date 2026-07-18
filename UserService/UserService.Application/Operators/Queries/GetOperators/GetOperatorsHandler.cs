using MediatR;
using UserService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace UserService.Application.Operators.Queries.GetOperators;

public sealed class GetOperatorsHandler : IRequestHandler<GetOperatorsQuery, IReadOnlyList<OperatorIdentityDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetOperatorsHandler> _logger;

    public GetOperatorsHandler(
        IIdentityService identityService,
        ILogger<GetOperatorsHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OperatorIdentityDto>> Handle(GetOperatorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _identityService.GetOperatorsAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogError(ex, "No fue posible consultar operadores en el proveedor de identidad. Se devolverá una lista vacía.");
            return Array.Empty<OperatorIdentityDto>();
        }
    }
}

