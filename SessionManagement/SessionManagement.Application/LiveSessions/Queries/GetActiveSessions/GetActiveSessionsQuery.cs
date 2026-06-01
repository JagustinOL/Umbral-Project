using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery : IRequest<IReadOnlyList<ActiveSessionDto>>;

