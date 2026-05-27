using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Missions.Commands.CreateMission;

public sealed class CreateMissionHandler : IRequestHandler<CreateMissionCommand, Guid>
{
    private readonly IMissionRepository _repository;

    public CreateMissionHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateMissionCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.TitleExistsAsync(request.Title, cancellationToken))
            throw new ConflictException($"Ya existe una misión con el título '{request.Title}'.");

        var difficulty = request.Difficulty switch
        {
            1 => DifficultyLevel.Easy,
            2 => DifficultyLevel.Medium,
            3 => DifficultyLevel.Hard,
            _ => throw new ArgumentOutOfRangeException(nameof(request.Difficulty),
                "Difficulty inválida. Valores esperados: 1=Easy, 2=Medium, 3=Hard.")
        };

        var mission = Mission.Create(
            title: request.Title,
            description: request.Description,
            difficulty: difficulty,
            maxDurationMinutes: request.MaxDurationMinutes);

        await _repository.SaveAsync(mission, cancellationToken);
        return mission.Id;
    }
}

