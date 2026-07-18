using MediatR;

namespace UserService.Application.Operators.Commands.DeactivateOperator;

public sealed record DeactivateOperatorCommand(Guid OperatorId) : IRequest;

