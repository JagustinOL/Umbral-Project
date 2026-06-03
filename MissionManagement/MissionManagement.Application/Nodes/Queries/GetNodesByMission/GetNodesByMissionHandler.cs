using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Queries.GetNodesByMission;

public sealed class GetNodesByMissionHandler : IRequestHandler<GetNodesByMissionQuery, IReadOnlyList<MissionNodeDto>>
{
    private readonly IMissionRepository _repository;

    public GetNodesByMissionHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MissionNodeDto>> Handle(GetNodesByMissionQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        return mission.Nodes
            .Where(n => n.NodeType == MissionNodeType.Stage)
            .OrderBy(n => n.ExecutionOrder)
            .Select(n => new MissionNodeDto(
                Id: n.Id,
                Title: n.Title,
                Description: n.Description,
                NodeType: n.NodeType.ToString(),
                ExecutionOrder: n.ExecutionOrder,
                BaseScore: n.BaseScore,
                ParentNodeId: n.ParentNodeId))
            .ToList();
    }
}

