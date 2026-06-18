using MediatR;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;

namespace SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;

public sealed class CancelLiveSessionHandler : IRequestHandler<CancelLiveSessionCommand>
{
    private readonly ISessionOperationFacade _facade;

    public CancelLiveSessionHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task Handle(CancelLiveSessionCommand request, CancellationToken cancellationToken) =>
        _facade.CancelSessionAsync(request.OperatorId, request.SessionId, cancellationToken);
}
