using MediatR;

namespace MissionManagement.Application.Missions.Commands.UpdateMissionDetails;

public sealed record UpdateMissionDetailsCommand(
    Guid Id,
    string Title,
    string Description,
    int? MaxDurationMinutes
) : IRequest;

