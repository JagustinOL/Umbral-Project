using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Queries.GetMissionById;

public sealed class GetMissionByIdHandler : IRequestHandler<GetMissionByIdQuery, MissionDto>
{
    private readonly IMissionRepository _repository;

    public GetMissionByIdHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<MissionDto> Handle(GetMissionByIdQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.Id}.");

        return new MissionDto(
            Id: mission.Id,
            Title: mission.Title,
            Description: mission.Description,
            Status: mission.Status.ToString(),
            Difficulty: mission.Difficulty.Name,
            MaxDurationMinutes: mission.MaxDurationMinutes,
            CreatedAtUtc: mission.CreatedAtUtc,
            LastModifiedAtUtc: mission.LastModifiedAtUtc,
            OperatorIds: mission.Operators.Select(x => x.OperatorId).ToList());
    }
}

