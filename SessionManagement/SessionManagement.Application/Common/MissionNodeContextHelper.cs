using SessionManagement.Application.Common.Interfaces;

namespace SessionManagement.Application.Common;

public static class MissionNodeContextHelper
{
    /// <summary>
    /// Etiqueta humana del juego: pregunta(s) de Trivia o instrucciones de Treasure Hunt.
    /// </summary>
    public static string? ResolvePrompt(PlayerNodeContentData? content)
    {
        if (content is null)
            return null;

        if (content.Questions is { Count: > 0 })
        {
            var prompts = content.Questions
                .Select(q => q.Prompt?.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Cast<string>()
                .ToList();
            return prompts.Count == 0 ? null : string.Join(" · ", prompts);
        }

        return string.IsNullOrWhiteSpace(content.Instructions)
            ? null
            : content.Instructions.Trim();
    }

    public static async Task<(string? NodeType, string? Prompt)> GetContextAsync(
        IMissionIntegrationService missionIntegration,
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await missionIntegration.GetNodePlayerContentAsync(
                missionId, nodeId, cancellationToken);
            return (content.NodeType, ResolvePrompt(content));
        }
        catch
        {
            return (null, null);
        }
    }
}
