using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

namespace SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionHandler : IRequestHandler<CreateLiveSessionCommand, CreatedLiveSessionDto>
{
    private readonly ISessionOperationFacade _facade;

    public CreateLiveSessionHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task<CreatedLiveSessionDto> Handle(CreateLiveSessionCommand request, CancellationToken cancellationToken) =>
        _facade.CreateSessionAsync(request.OperatorId, request.MissionId, cancellationToken);
}
