using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;

public sealed record SubmitTriviaAnswerCommand(
    Guid SessionId,
    Guid TeamId,
    Guid NodeId,
    string Answer,
    int QuestionIndex
) : IRequest<SubmissionResultDto>;
