using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Facades;
using SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;

namespace SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;

public sealed class SubmitTriviaAnswerHandler : IRequestHandler<SubmitTriviaAnswerCommand, SubmissionResultDto>
{
    private readonly ISessionOperationFacade _facade;

    public SubmitTriviaAnswerHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task<SubmissionResultDto> Handle(SubmitTriviaAnswerCommand request, CancellationToken cancellationToken) =>
        _facade.SubmitTriviaAsync(
            request.SessionId,
            request.TeamId,
            request.NodeId,
            request.Answer,
            cancellationToken);
}
