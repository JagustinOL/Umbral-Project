using MediatR;
using UserService.Domain.Repositories;

namespace UserService.Application.Operators.Queries.IsActiveOperator;

public sealed class IsActiveOperatorHandler : IRequestHandler<IsActiveOperatorQuery, bool>
{
    private readonly IUserRepository _userRepository;

    public IsActiveOperatorHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<bool> Handle(IsActiveOperatorQuery request, CancellationToken cancellationToken) =>
        _userRepository.IsActiveOperatorAsync(request.OperatorId, cancellationToken);
}
