using MissionManagement.Application.Admins.Commands.CreateAdmin;
using MissionManagement.WebApi.Contracts.Admins;

namespace MissionManagement.WebApi.Mapping;

public static class AdminMappings
{
    public static CreateAdminCommand ToCommand(this CreateAdminRequest body) =>
        new(
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email,
            Password: body.Password);
}
