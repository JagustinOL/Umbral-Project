using MediatR;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;

namespace SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;

public sealed class StartLiveSessionHandler : IRequestHandler<StartLiveSessionCommand>
{
    private readonly ISessionOperationFacade _facade;

    public StartLiveSessionHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task Handle(StartLiveSessionCommand request, CancellationToken cancellationToken) =>
        _facade.StartSessionAsync(request.OperatorId, request.SessionId, cancellationToken);
}
