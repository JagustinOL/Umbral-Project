using MissionManagement.Application.Operators.Commands.CreateOperator;
using MissionManagement.Application.Operators.Commands.DeactivateOperator;
using MissionManagement.WebApi.Contracts.Operators;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

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
