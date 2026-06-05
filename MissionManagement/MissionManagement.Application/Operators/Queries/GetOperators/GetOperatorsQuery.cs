using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Operators.Queries.GetOperators;

public sealed record GetOperatorsQuery : IRequest<IReadOnlyList<OperatorIdentityDto>>;

