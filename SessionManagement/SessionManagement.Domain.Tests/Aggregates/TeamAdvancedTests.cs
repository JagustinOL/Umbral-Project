using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class TeamAdvancedTests
{
    [Fact]
    public void Disband_WhenLeader_DisbandsTeam()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Gamma", leaderId, "Líder");

        team.Disband(leaderId);

        team.IsDisbanded.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_WhenLeader_ChangesName()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Gamma", leaderId, "Líder");

        team.UpdateName("Gamma II", leaderId);

        team.Name.Should().Be("Gamma II");
    }

    [Fact]
    public void TeamCode_Generate_CreatesValidCode()
    {
        var code = TeamCode.Generate();

        code.Value.Should().NotBeNullOrWhiteSpace();
    }
}
