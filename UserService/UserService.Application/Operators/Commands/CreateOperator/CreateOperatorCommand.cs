using MediatR;
using UserService.Application.Common;

namespace UserService.Application.Operators.Commands.CreateOperator;

public sealed record CreateOperatorCommand(
    string FirstName,
    string LastName,
    string Email
) : IRequest<CreateOperatorResult>;
