using MediatR;

namespace SessionManagement.Application.OperatorSessions.Queries.OperatorIsSupervisingMission;

public sealed record OperatorIsSupervisingMissionQuery(Guid OperatorId, Guid MissionId) : IRequest<bool>;
