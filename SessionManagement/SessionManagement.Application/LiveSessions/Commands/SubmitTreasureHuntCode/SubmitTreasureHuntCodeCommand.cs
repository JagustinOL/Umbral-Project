using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;

public sealed record SubmitTreasureHuntCodeCommand(
    Guid SessionId,
    Guid TeamId,
    Guid NodeId,
    string FoundCode
) : IRequest<SubmissionResultDto>;

