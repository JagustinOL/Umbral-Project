using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Facades;
using SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;

namespace SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;

public sealed class SubmitTreasureHuntCodeHandler : IRequestHandler<SubmitTreasureHuntCodeCommand, SubmissionResultDto>
{
    private readonly ISessionOperationFacade _facade;

    public SubmitTreasureHuntCodeHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task<SubmissionResultDto> Handle(SubmitTreasureHuntCodeCommand request, CancellationToken cancellationToken) =>
        _facade.SubmitTreasureHuntAsync(
            request.SessionId,
            request.TeamId,
            request.NodeId,
            request.FoundCode,
            cancellationToken);
}
