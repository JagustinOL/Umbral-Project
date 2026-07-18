using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Commands.JoinSession;

public sealed record JoinSessionCommand(
    string JoinCode,
    Guid TeamId) : IRequest<JoinSessionResultDto>;
