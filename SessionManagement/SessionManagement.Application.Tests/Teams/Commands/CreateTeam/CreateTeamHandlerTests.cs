using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.CreateTeam;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.CreateTeam;

public sealed class CreateTeamHandlerTests
{
    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.ExistsByNameAsync("EquipoX", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateTeamHandler(teamRepositoryMock.Object);
        var command = new CreateTeamCommand("EquipoX", Guid.NewGuid(), "Leader");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenCreatorAlreadyInTeam_ThrowsConflictException()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var existingTeam = SessionManagement.Domain.Aggregates.Team.Create(
            "EquipoExistente",
            creatorId,
            "Lider");

        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTeam);

        var handler = new CreateTeamHandler(teamRepositoryMock.Object);
        var command = new CreateTeamCommand("EquipoNuevo", creatorId, "Lider");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ya pertenece a un equipo activo*");
    }

    [Fact]
    public async Task Handle_WhenValid_PersistsTeam()
    {
        // Arrange
        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamWithPendingJoinRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);
        teamRepositoryMock
            .Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        teamRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateTeamHandler(teamRepositoryMock.Object);
        var command = new CreateTeamCommand("EquipoNuevo", Guid.NewGuid(), "Leader");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        teamRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<SessionManagement.Domain.Aggregates.Team>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
