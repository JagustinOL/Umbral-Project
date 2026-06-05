using MediatR;
using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Admins.Commands.CreateAdmin;

public sealed class CreateAdminHandler : IRequestHandler<CreateAdminCommand, Guid>
{
    private readonly IIdentityService _identityService;

    public CreateAdminHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Guid> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        PasswordValidation.EnsureValid(request.Password);

        return await _identityService.CreateAdminAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);
    }
}
