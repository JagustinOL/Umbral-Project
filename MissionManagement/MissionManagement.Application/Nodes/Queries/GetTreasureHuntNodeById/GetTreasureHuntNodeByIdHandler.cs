using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;

public sealed class GetTreasureHuntNodeByIdHandler : IRequestHandler<GetTreasureHuntNodeByIdQuery, TreasureHuntNodeDto>
{
    private readonly IMissionRepository _repository;

    public GetTreasureHuntNodeByIdHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TreasureHuntNodeDto> Handle(GetTreasureHuntNodeByIdQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");
        if (node.NodeType != MissionNodeType.TreasureHunt)
            throw new ConflictException($"El nodo con Id={request.NodeId} no es de tipo TreasureHunt.");
        if (node.ParentNodeId is null)
            throw new ConflictException($"El nodo TreasureHunt con Id={request.NodeId} no tiene ParentNodeId.");
        if (node.Destination is null)
            throw new ConflictException($"El nodo TreasureHunt con Id={request.NodeId} no tiene coordenadas configuradas.");

        return new TreasureHuntNodeDto(
            Id: node.Id,
            MissionId: mission.Id,
            ParentNodeId: node.ParentNodeId.Value,
            NodeType: node.NodeType.ToString(),
            ExecutionOrder: node.ExecutionOrder,
            BaseScore: node.BaseScore,
            Instructions: node.Instructions ?? string.Empty,
            SecretCode: node.SecretCode ?? string.Empty,
            Destination: new GpsCoordinateDto(
                Latitude: node.Destination.Latitude,
                Longitude: node.Destination.Longitude)
        );
    }
}

