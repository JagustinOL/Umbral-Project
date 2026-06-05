using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.Common;

namespace MissionManagement.Application.Missions.Commands.ActivateMission;

public sealed class ActivateMissionHandler : IRequestHandler<ActivateMissionCommand>
{
    private readonly IMissionRepository _repository;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public ActivateMissionHandler(
        IMissionRepository repository,
        IDomainEventPublisher domainEventPublisher)
    {
        _repository = repository;
        _domainEventPublisher = domainEventPublisher;
    }

    public async Task Handle(ActivateMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.Id}.");

        mission.Activate();
        await _repository.SaveAsync(mission, cancellationToken);

        var domainEvents = mission.DomainEvents.ToList();
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);
        mission.ClearDomainEvents();
    }
}
