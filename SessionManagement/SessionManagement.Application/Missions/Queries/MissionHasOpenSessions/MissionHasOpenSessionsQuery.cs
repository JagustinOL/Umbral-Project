using MediatR;

namespace SessionManagement.Application.Missions.Queries.MissionHasOpenSessions;

public sealed record MissionHasOpenSessionsQuery(Guid MissionId) : IRequest<bool>;
