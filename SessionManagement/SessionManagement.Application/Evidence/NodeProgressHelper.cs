using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence;

public static class NodeProgressHelper
{
    public static bool IsNodeCompleted(
        LiveSession session,
        Guid teamId,
        NodeValidationRule rule)
    {
        if (rule.ValidationType == NodeValidationType.TreasureHunt)
        {
            return session.EvidenceSubmissions.Any(e =>
                e.TeamId == teamId &&
                e.MissionNodeId == rule.NodeId &&
                e.IsValid == true);
        }

        var answeredIndices = session.EvidenceSubmissions
            .Where(e =>
                e.TeamId == teamId &&
                e.MissionNodeId == rule.NodeId &&
                e.IsValid == true &&
                e.QuestionIndex.HasValue)
            .Select(e => e.QuestionIndex!.Value)
            .ToHashSet();

        return answeredIndices.Count >= rule.ExpectedAnswers.Count;
    }

    public static int GetNextQuestionIndex(
        LiveSession session,
        Guid teamId,
        Guid nodeId,
        int totalQuestions)
    {
        var answeredIndices = session.EvidenceSubmissions
            .Where(e =>
                e.TeamId == teamId &&
                e.MissionNodeId == nodeId &&
                e.IsValid == true &&
                e.QuestionIndex.HasValue)
            .Select(e => e.QuestionIndex!.Value)
            .ToHashSet();

        for (var index = 0; index < totalQuestions; index++)
        {
            if (!answeredIndices.Contains(index))
                return index;
        }

        return totalQuestions;
    }

    public static HashSet<Guid> GetCompletedNodeIds(
        LiveSession session,
        Guid teamId,
        IReadOnlyList<NodeValidationRule> rules)
    {
        var completed = new HashSet<Guid>();
        foreach (var rule in rules)
        {
            if (IsNodeCompleted(session, teamId, rule))
                completed.Add(rule.NodeId);
        }

        return completed;
    }
}
