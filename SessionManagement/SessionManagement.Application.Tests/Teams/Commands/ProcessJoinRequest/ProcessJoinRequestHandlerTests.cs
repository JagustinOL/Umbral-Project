using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.ProcessJoinRequest;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.ProcessJoinRequest;

public sealed class ProcessJoinRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestorIdIsEmpty_ThrowsArgumentException()
    {
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var handler = new ProcessJoinRequestHandler(teamRepositoryMock.Object);
        var command = new ProcessJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), true, Guid.Empty);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("El parámetro requestorId es obligatorio.");
        teamRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRequestorIsNotLeader_ThrowsConflictException()
    {
        // Arrange
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var leaderId = Guid.NewGuid();
        var nonLeaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Leader");
        team.SubmitJoinRequest(Guid.NewGuid(), "Nuevo");
        team.AddMember(SessionManagement.Domain.Entities.TeamMember.Create(nonLeaderId, "Member2"));

        var requestId = team.JoinRequests.Single().Id;

        teamRepositoryMock
            .Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new ProcessJoinRequestHandler(teamRepositoryMock.Object);
        var command = new ProcessJoinRequestCommand(team.Id, requestId, true, nonLeaderId);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenApprovingPlayerAlreadyInAnotherTeam_ThrowsConflictException()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var team = Team.Create("EquipoDestino", leaderId, "Leader");
        team.SubmitJoinRequest(applicantId, "Applicant");

        var existingTeam = Team.Create("EquipoOrigen", applicantId, "ApplicantLeader");
        var requestId = team.JoinRequests.Single().Id;

        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTeam);

        var handler = new ProcessJoinRequestHandler(teamRepositoryMock.Object);
        var command = new ProcessJoinRequestCommand(team.Id, requestId, true, leaderId);

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
