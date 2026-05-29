using MediatR;

namespace MissionManagement.Application.Operators.Commands.DeactivateOperator;

public sealed record DeactivateOperatorCommand(Guid OperatorId) : IRequest;

