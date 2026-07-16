using UserService.Domain.Enums;

namespace UserService.Application.Common;

public static class UserRoleNames
{
    public static string FromDomainRole(UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Operator => "operator",
        UserRole.Player => "player",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Rol de usuario no soportado.")
    };
}
