using MediatR;

namespace SessionManagement.Application.Teams.Commands.SubmitJoinRequest;

public sealed record SubmitJoinRequestCommand(
    string TeamCode,
    Guid PlayerId,
    string DisplayName
) : IRequest<SubmitJoinRequestResult>;
