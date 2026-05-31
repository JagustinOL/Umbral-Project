using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.UpdateMissionDetails;

public sealed class UpdateMissionDetailsHandler : IRequestHandler<UpdateMissionDetailsCommand>
{
    private readonly IMissionRepository _repository;

    public UpdateMissionDetailsHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateMissionDetailsCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.Id}.");

        mission.UpdateDetails(request.Title, request.Description, request.MaxDurationMinutes);

        await _repository.SaveAsync(mission, cancellationToken);
    }
}

