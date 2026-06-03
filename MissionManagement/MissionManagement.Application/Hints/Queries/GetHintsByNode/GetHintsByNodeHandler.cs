using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints.Queries.GetHintsByNode;

public sealed class GetHintsByNodeHandler : IRequestHandler<GetHintsByNodeQuery, IReadOnlyList<HintDto>>
{
    private readonly IMissionRepository _repository;

    public GetHintsByNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<HintDto>> Handle(GetHintsByNodeQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");

        return node.Hints
            .OrderBy(h => h.Order)
            .Select(h => new HintDto(
                Id: h.Id,
                Order: h.Order,
                Content: h.Content,
                PenaltyPoints: h.PenaltyPoints))
            .ToList();
    }
}

