using FluentAssertions;
using UserService.Application.Common;
using UserService.Domain.Enums;

namespace UserService.Application.Tests.Common;

public sealed class UserRoleNamesTests
{
    [Theory]
    [InlineData(UserRole.Admin, "admin")]
    [InlineData(UserRole.Operator, "operator")]
    [InlineData(UserRole.Player, "player")]
    public void FromDomainRole_MapsToLowercaseKeycloakNames(UserRole role, string expected)
    {
        UserRoleNames.FromDomainRole(role).Should().Be(expected);
    }
}
