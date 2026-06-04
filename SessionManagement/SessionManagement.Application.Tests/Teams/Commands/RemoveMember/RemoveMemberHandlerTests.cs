using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.RemoveMember;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.RemoveMember;

public sealed class RemoveMemberHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_RemovesMember()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Líder");
        team.AddMember(TeamMember.Create(memberId, "Miembro", TeamMemberRole.Member));
        var repo = new Mock<ITeamRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var handler = new RemoveMemberHandler(repo.Object);
        await handler.Handle(new RemoveMemberCommand(team.Id, memberId, leaderId), CancellationToken.None);

        team.Members.Should().ContainSingle(m => m.PlayerRef == leaderId);
        repo.Verify(x => x.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }
}
