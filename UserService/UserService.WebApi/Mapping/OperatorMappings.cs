using UserService.Application.Operators.Commands.CreateOperator;
using UserService.Application.Operators.Commands.DeactivateOperator;
using UserService.WebApi.Contracts.Operators;
using UserService.WebApi.Contracts.Routes;

namespace UserService.WebApi.Mapping;

public static class OperatorMappings
{
    public static CreateOperatorCommand ToCommand(this CreateOperatorRequest body) =>
        new(
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email);

    public static DeactivateOperatorCommand ToCommand(this OperatorRoute route) =>
        new(route.OperatorId);
}
