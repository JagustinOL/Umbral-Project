using System.Security.Claims;
using FluentAssertions;
using MissionManagement.WebApi.Auth;

namespace MissionManagement.WebApi.Tests.Auth;

public sealed class UserServiceAuthRoleClaimsTests
{
    [Fact]
    public void BuildRoleClaims_UsesClaimTypesRoleWithLowercaseValues()
    {
        var claims = UserServiceValidatedUserClaims.BuildRoleClaims("Operator", ["operator", "ADMIN"]).ToList();

        claims.Should().AllSatisfy(claim => claim.Type.Should().Be(ClaimTypes.Role));
        claims.Select(claim => claim.Value).Should().BeEquivalentTo(["operator", "admin"]);
    }
}
