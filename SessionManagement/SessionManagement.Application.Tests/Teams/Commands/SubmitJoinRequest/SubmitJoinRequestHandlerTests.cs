using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.SubmitJoinRequest;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.SubmitJoinRequest;

public sealed class SubmitJoinRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenPlayerAlreadyInAnotherTeam_ThrowsConflictException()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var existingTeam = Team.Create("EquipoA", playerId, "Lider");
        var targetTeam = Team.Create("EquipoB", Guid.NewGuid(), "OtroLider");

        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetTeam);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTeam);

        var handler = new SubmitJoinRequestHandler(teamRepositoryMock.Object);
        var command = new SubmitJoinRequestCommand(
            targetTeam.Code.Value,
            playerId,
            "Jugador");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ya pertenece a un equipo activo*");
        teamRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
