using FluentAssertions;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Teams.Commands.SubmitJoinRequest;

namespace SessionManagement.Application.Tests.Dtos;

public sealed class DtoCoverageTests
{
    [Fact]
    public void AllDtos_CanBeConstructed()
    {
        var id = Guid.NewGuid();
        _ = new ActiveSessionDto(id, id, "CODE", "Active", DateTime.UtcNow);
        _ = new CreatedLiveSessionDto(id, "CODE");
        _ = new CreatedTeamDto(id, "Team", "CODE");
        _ = new JoinRequestDto(id, id, "P", "Pending", DateTime.UtcNow, null);
        _ = new OperatorAssignedMissionDto(id, id, "M");
        _ = new OperatorOpenSessionDto(id, id, "CODE", "Active");
        _ = new PlayerTeamMembershipDto(true, id, "Team", "CODE", "Leader", false, null);
        _ = new SessionTeamsDto(id, [id], 1);
        _ = new SubmissionResultDto(true, id, id, 100, 0, 1, true);
        _ = new TeamCurrentStageDto(id, id, id, "Trivia", 1, false);
        _ = new TeamCurrentNodeContentDto(id, "Trivia", [new PlayerTriviaQuestionDto("Q", ["A"])], null, 0, 1);
        _ = new TeamDetailsDto(id, "T", "CODE", false, null, false, [new TeamMemberDto(id, "M", "Leader", DateTime.UtcNow)]);
        _ = new SubmitJoinRequestResult(id, id);
        _ = new AssignedMissionData(id, id, "M");
        _ = new MissionNodeValidationData(id, "Trivia", 1, 10, ["A"]);
        _ = new PlayerNodeContentData(id, "Trivia", [new PlayerTriviaQuestionData("Q", ["A"])], null);
    }
}
