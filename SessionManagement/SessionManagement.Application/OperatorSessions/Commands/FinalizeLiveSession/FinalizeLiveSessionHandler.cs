using MediatR;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;

namespace SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;

public sealed class FinalizeLiveSessionHandler : IRequestHandler<FinalizeLiveSessionCommand>
{
    private readonly ISessionOperationFacade _facade;

    public FinalizeLiveSessionHandler(ISessionOperationFacade facade)
    {
        _facade = facade;
    }

    public Task Handle(FinalizeLiveSessionCommand request, CancellationToken cancellationToken) =>
        _facade.FinalizeSessionAsync(request.OperatorId, request.SessionId, cancellationToken);
}
