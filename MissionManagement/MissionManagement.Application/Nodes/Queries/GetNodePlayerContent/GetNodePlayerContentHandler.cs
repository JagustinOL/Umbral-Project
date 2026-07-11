using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Queries.GetNodePlayerContent;

public sealed class GetNodePlayerContentHandler
    : IRequestHandler<GetNodePlayerContentQuery, PlayerNodeContentDto>
{
    private readonly IMissionRepository _repository;

    public GetNodePlayerContentHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerNodeContentDto> Handle(
        GetNodePlayerContentQuery request,
        CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");

        if (node.NodeType == MissionNodeType.Trivia)
        {
            if (node.TriviaQuestions.Count == 0)
                throw new ConflictException($"El nodo Trivia con Id={request.NodeId} no tiene preguntas configuradas.");

            return new PlayerNodeContentDto(
                NodeId: node.Id,
                NodeType: node.NodeType.ToString(),
                Questions: node.TriviaQuestions
                    .Select(q => new PlayerTriviaQuestionDto(
                        Prompt: q.Prompt,
                        Options: q.Options))
                    .ToList(),
                Instructions: null);
        }

        if (node.NodeType == MissionNodeType.TreasureHunt)
        {
            if (string.IsNullOrWhiteSpace(node.Instructions))
                throw new ConflictException($"El nodo TreasureHunt con Id={request.NodeId} no tiene instrucciones configuradas.");

            return new PlayerNodeContentDto(
                NodeId: node.Id,
                NodeType: node.NodeType.ToString(),
                Questions: null,
                Instructions: node.Instructions);
        }

        throw new ConflictException(
            $"El nodo con Id={request.NodeId} no expone contenido de jugador (tipo {node.NodeType}).");
    }
}
