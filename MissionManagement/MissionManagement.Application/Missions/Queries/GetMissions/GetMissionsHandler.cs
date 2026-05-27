using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Queries.GetMissions;

public sealed class GetMissionsHandler : IRequestHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>
{
    private readonly IMissionRepository _repository;

    public GetMissionsHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MissionDto>> Handle(GetMissionsQuery request, CancellationToken cancellationToken)
    {
        var missions = await _repository.GetAllAsync(cancellationToken);

        return missions
            .Select(m => new MissionDto(
                Id: m.Id,
                Title: m.Title,
                Description: m.Description,
                Status: m.Status.ToString(),
                Difficulty: m.Difficulty.Name,
                MaxDurationMinutes: m.MaxDurationMinutes,
                CreatedAtUtc: m.CreatedAtUtc,
                LastModifiedAtUtc: m.LastModifiedAtUtc))
            .ToList();
    }
}

