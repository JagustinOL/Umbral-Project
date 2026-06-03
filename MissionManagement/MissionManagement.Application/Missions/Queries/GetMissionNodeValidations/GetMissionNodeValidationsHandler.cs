using MediatR;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;

public sealed class GetMissionNodeValidationsHandler
    : IRequestHandler<GetMissionNodeValidationsQuery, IReadOnlyList<MissionNodeValidationDto>>
{
    private readonly IMissionRepository _repository;

    public GetMissionNodeValidationsHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MissionNodeValidationDto>> Handle(
        GetMissionNodeValidationsQuery request,
        CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var leaves = mission.Nodes
            .OrderBy(n => n.ExecutionOrder)
            .SelectMany(stage => stage.Children.OrderBy(c => c.ExecutionOrder))
            .Where(n => n.NodeType is MissionNodeType.Trivia or MissionNodeType.TreasureHunt)
            .ToList();

        int order = 1;
        var result = new List<MissionNodeValidationDto>(capacity: leaves.Count);

        foreach (var node in leaves)
        {
            result.Add(new MissionNodeValidationDto(
                NodeId: node.Id,
                NodeType: node.NodeType.ToString(),
                ExecutionOrder: order++,
                BaseScore: node.BaseScore,
                ExpectedValue: ResolveExpectedValue(node)));
        }

        return result;
    }

    private static string ResolveExpectedValue(MissionNode node)
    {
        if (node.NodeType == MissionNodeType.TreasureHunt)
        {
            if (string.IsNullOrWhiteSpace(node.SecretCode))
                throw new ConflictException($"El nodo TreasureHunt con Id={node.Id} no tiene SecretCode configurado.");

            return node.SecretCode;
        }

        if (node.NodeType == MissionNodeType.Trivia)
        {
            var first = node.TriviaQuestions.FirstOrDefault()
                ?? throw new ConflictException($"El nodo Trivia con Id={node.Id} no tiene preguntas configuradas.");

            if (first.CorrectOptionIndex < 0 || first.CorrectOptionIndex >= first.Options.Count)
                throw new ConflictException(
                    $"El nodo Trivia con Id={node.Id} tiene CorrectOptionIndex inválido ({first.CorrectOptionIndex}).");

            return first.Options[first.CorrectOptionIndex];
        }

        throw new ConflictException($"Tipo de nodo no soportado para validación: {node.NodeType}.");
    }
}

