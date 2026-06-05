using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Queries.GetGamesByStage;

public sealed class GetGamesByStageHandler : IRequestHandler<GetGamesByStageQuery, IReadOnlyList<StageGameDto>>
{
    private readonly IMissionRepository _repository;

    public GetGamesByStageHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StageGameDto>> Handle(GetGamesByStageQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var stage = mission.FindNodeById(request.StageId);
        if (stage is null)
            throw new NotFoundException($"No se encontró la etapa con Id={request.StageId}.");

        if (stage.NodeType != MissionNodeType.Stage)
            throw new NotFoundException($"El nodo {request.StageId} no es una etapa (Stage).");

        return stage.Children
            .OrderBy(c => c.ExecutionOrder)
            .Select(c => new StageGameDto(
                Id: c.Id,
                NodeType: c.NodeType.ToString(),
                ExecutionOrder: c.ExecutionOrder,
                BaseScore: c.BaseScore,
                Title: c.Title))
            .ToList();
    }
}
