using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.CreateTeam;

public sealed class CreateTeamHandler : IRequestHandler<CreateTeamCommand, Guid>
{
    private const int MaxGenerateCodeRetries = 5;
    private readonly ITeamRepository _teamRepository;

    public CreateTeamHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        await PlayerSingleTeamGuard.EnsureCanJoinOrCreateTeamAsync(
            _teamRepository,
            request.CreatorId,
            cancellationToken: cancellationToken);

        var creatorDisplayName = string.IsNullOrWhiteSpace(request.CreatorDisplayName)
            ? $"player-{request.CreatorId.ToString("N")[..8]}"
            : request.CreatorDisplayName.Trim();

        var alreadyExists = await _teamRepository.ExistsByNameAsync(
            request.Name,
            cancellationToken: cancellationToken);
        if (alreadyExists)
            throw new ConflictException($"Ya existe un equipo con nombre '{request.Name}'.");

        var team = Team.Create(request.Name, request.CreatorId, creatorDisplayName);

        var attempt = 0;
        while (attempt < MaxGenerateCodeRetries)
        {
            var codeExists = await _teamRepository.ExistsByCodeAsync(
                team.Code.Value,
                cancellationToken: cancellationToken);
            if (!codeExists)
                break;

            team.RegenerateCode();
            attempt++;
        }

        if (attempt == MaxGenerateCodeRetries)
            throw new ConflictException("No fue posible generar un código único para el equipo.");

        await _teamRepository.SaveAsync(team, cancellationToken);
        return team.Id;
    }
}
