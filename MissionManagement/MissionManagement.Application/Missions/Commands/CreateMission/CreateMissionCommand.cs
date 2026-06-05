using MediatR;

namespace MissionManagement.Application.Missions.Commands.CreateMission;

public sealed record CreateMissionCommand(
    string Title,
    string Description,
    int Difficulty,
    int? MaxDurationMinutes
) : IRequest<Guid>;

