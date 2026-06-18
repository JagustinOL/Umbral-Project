using MediatR;
using MissionManagement.Application.Common;

namespace MissionManagement.Application.Operators.Commands.CreateOperator;

public sealed record CreateOperatorCommand(
    string FirstName,
    string LastName,
    string Email
) : IRequest<CreateOperatorResult>;
