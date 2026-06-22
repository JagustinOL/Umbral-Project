using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Operators.Queries.GetOperators;

public sealed record GetOperatorsQuery : IRequest<IReadOnlyList<OperatorIdentityDto>>;

