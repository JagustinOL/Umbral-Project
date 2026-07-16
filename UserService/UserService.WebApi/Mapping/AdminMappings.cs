using UserService.Application.Admins.Commands.CreateAdmin;
using UserService.WebApi.Contracts.Admins;

namespace UserService.WebApi.Mapping;

public static class AdminMappings
{
    public static CreateAdminCommand ToCommand(this CreateAdminRequest body) =>
        new(
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email,
            Password: body.Password);
}
