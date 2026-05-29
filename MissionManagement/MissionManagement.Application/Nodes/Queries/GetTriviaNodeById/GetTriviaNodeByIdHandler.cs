using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Queries.GetTriviaNodeById;

public sealed class GetTriviaNodeByIdHandler : IRequestHandler<GetTriviaNodeByIdQuery, TriviaNodeDto>
{
    private readonly IMissionRepository _repository;

    public GetTriviaNodeByIdHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TriviaNodeDto> Handle(GetTriviaNodeByIdQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");
        if (node.NodeType != MissionNodeType.Trivia)
            throw new ConflictException($"El nodo con Id={request.NodeId} no es de tipo Trivia.");

        return new TriviaNodeDto(
            Id: node.Id,
            MissionId: mission.Id,
            ParentNodeId: node.ParentNodeId ?? Guid.Empty,
            NodeType: node.NodeType.ToString(),
            ExecutionOrder: node.ExecutionOrder,
            BaseScore: node.BaseScore,
            Questions: node.TriviaQuestions
                .Select(q => new TriviaQuestionDto(
                    Prompt: q.Prompt,
                    Options: q.Options,
                    CorrectOptionIndex: q.CorrectOptionIndex))
                .ToList()
        );
    }
}

