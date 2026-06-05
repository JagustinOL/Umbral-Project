using MediatR;

namespace MissionManagement.Application.Missions.Commands.ActivateMission;

public sealed record ActivateMissionCommand(Guid Id) : IRequest;
