using MediatR;

namespace MissionManagement.Application.Missions.Commands.DeactivateMission;

public sealed record DeactivateMissionCommand(Guid Id) : IRequest;

