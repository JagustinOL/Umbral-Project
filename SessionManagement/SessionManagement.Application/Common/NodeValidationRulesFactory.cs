using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Common;

public static class NodeValidationRulesFactory
{
    public static async Task<IReadOnlyList<NodeValidationRule>> BuildAsync(
        IMissionIntegrationService missionIntegration,
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var data = await missionIntegration.GetNodeValidationDataAsync(missionId, cancellationToken);
        return data
            .Select(x => new NodeValidationRule(
                NodeId: x.NodeId,
                ExecutionOrder: x.ExecutionOrder,
                ValidationType: ParseType(x.NodeType),
                ExpectedAnswers: x.ExpectedAnswers))
            .OrderBy(x => x.ExecutionOrder)
            .ToList();
    }

    public static NodeValidationType ParseType(string rawType)
    {
        if (string.Equals(rawType, "Trivia", StringComparison.OrdinalIgnoreCase))
            return NodeValidationType.Trivia;
        if (string.Equals(rawType, "TreasureHunt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawType, "Treasure_Hunt", StringComparison.OrdinalIgnoreCase))
            return NodeValidationType.TreasureHunt;

        throw new InvalidOperationException($"Tipo de nodo no soportado: '{rawType}'.");
    }
}
