using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Queries.GetTeamById;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Queries.GetTeamById;

public sealed class GetTeamByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<ITeamRepository>();
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var handler = new GetTeamByIdHandler(repo.Object);
        var act = () => handler.Handle(new GetTeamByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        var team = Team.Create("Equipo", Guid.NewGuid(), "Líder");
        var repo = new Mock<ITeamRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var handler = new GetTeamByIdHandler(repo.Object);
        var result = await handler.Handle(new GetTeamByIdQuery(team.Id), CancellationToken.None);

        result.TeamId.Should().Be(team.Id);
        result.Name.Should().Be("Equipo");
    }
}
