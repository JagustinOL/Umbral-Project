using MediatR;
using UserService.Application.Common;

namespace UserService.Application.Operators.Commands.ResendOperatorActivation;

public sealed record ResendOperatorActivationCommand(Guid OperatorId)
    : IRequest<ResendOperatorActivationResult>;
