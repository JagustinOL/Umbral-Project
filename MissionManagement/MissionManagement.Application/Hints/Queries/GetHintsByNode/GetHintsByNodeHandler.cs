using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Hints;

namespace MissionManagement.Application.Hints.Queries.GetHintsByNode;

public sealed class GetHintsByNodeHandler : IRequestHandler<GetHintsByNodeQuery, IReadOnlyList<HintDto>>
{
    private readonly IHintAccessService _hintAccessService;

    public GetHintsByNodeHandler(IHintAccessService hintAccessService)
    {
        _hintAccessService = hintAccessService;
    }

    public Task<IReadOnlyList<HintDto>> Handle(GetHintsByNodeQuery request, CancellationToken cancellationToken) =>
        _hintAccessService.GetHintsForNodeAsync(request.MissionId, request.NodeId, cancellationToken);
}

