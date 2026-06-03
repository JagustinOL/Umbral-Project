using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.ProcessJoinRequest;

public sealed class ProcessJoinRequestHandler : IRequestHandler<ProcessJoinRequestCommand>
{
    private readonly ITeamRepository _teamRepository;

    public ProcessJoinRequestHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(ProcessJoinRequestCommand request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        if (!team.IsLeader(request.RequestorId))
            throw new ConflictException("Solo el líder del equipo puede procesar solicitudes.");

        if (request.IsApproved)
        {
            var joinRequest = team.JoinRequests.FirstOrDefault(x => x.Id == request.RequestId);
            if (joinRequest is null)
                throw new NotFoundException("No se encontró la solicitud de unión.");

            if (joinRequest.Status != JoinRequestStatus.Pending)
                throw new ConflictException("La solicitud ya fue procesada.");

            await PlayerSingleTeamGuard.EnsureCanJoinOrCreateTeamAsync(
                _teamRepository,
                joinRequest.PlayerRef,
                targetTeamId: team.Id,
                cancellationToken);
        }

        team.ProcessJoinRequest(request.RequestId, request.IsApproved);
        await _teamRepository.SaveAsync(team, cancellationToken);
    }
}
