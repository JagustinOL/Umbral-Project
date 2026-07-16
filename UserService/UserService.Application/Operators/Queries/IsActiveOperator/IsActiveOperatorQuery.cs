using MediatR;

namespace UserService.Application.Operators.Queries.IsActiveOperator;

public sealed record IsActiveOperatorQuery(Guid OperatorId) : IRequest<bool>;
