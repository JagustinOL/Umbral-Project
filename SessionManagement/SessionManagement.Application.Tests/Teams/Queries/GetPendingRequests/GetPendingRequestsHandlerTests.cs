using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Queries.GetPendingRequests;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Queries.GetPendingRequests;

public sealed class GetPendingRequestsHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestorIdIsEmpty_ThrowsArgumentException()
    {
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var handler = new GetPendingRequestsHandler(teamRepositoryMock.Object);
        var query = new GetPendingRequestsQuery(Guid.NewGuid(), Guid.Empty);

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("El parámetro requestorId es obligatorio.");
        teamRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRequestorIsLeader_ReturnsPendingRequests()
    {
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var leaderId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Leader");
        team.SubmitJoinRequest(applicantId, "Applicant");

        teamRepositoryMock
            .Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new GetPendingRequestsHandler(teamRepositoryMock.Object);
        var query = new GetPendingRequestsQuery(team.Id, leaderId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].PlayerRef.Should().Be(applicantId);
        result[0].DisplayName.Should().Be("Applicant");
    }

    [Fact]
    public async Task Handle_WhenRequestorIsNotLeader_ThrowsConflictException()
    {
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var leaderId = Guid.NewGuid();
        var nonLeaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Leader");

        teamRepositoryMock
            .Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new GetPendingRequestsHandler(teamRepositoryMock.Object);
        var query = new GetPendingRequestsQuery(team.Id, nonLeaderId);

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
