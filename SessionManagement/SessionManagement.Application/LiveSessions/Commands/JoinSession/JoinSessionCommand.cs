using MediatR;

namespace SessionManagement.Application.LiveSessions.Commands.JoinSession;

public sealed record JoinSessionCommand(
    string JoinCode,
    Guid TeamId
) : IRequest<Guid>;

