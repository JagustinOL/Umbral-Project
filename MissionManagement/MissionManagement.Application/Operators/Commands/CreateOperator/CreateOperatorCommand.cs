using MediatR;

namespace MissionManagement.Application.Operators.Commands.CreateOperator;

public sealed record CreateOperatorCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<Guid>;

