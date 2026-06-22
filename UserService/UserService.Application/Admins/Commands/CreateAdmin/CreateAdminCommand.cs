using MediatR;

namespace UserService.Application.Admins.Commands.CreateAdmin;

public sealed record CreateAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<Guid>;
