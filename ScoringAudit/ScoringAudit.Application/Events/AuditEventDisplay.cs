using System.Globalization;
using System.Text.Json;

namespace ScoringAudit.Application.Events;

/// <summary>
/// Construye títulos, líneas de detalle y metadata legible para eventos de auditoría.
/// </summary>
internal static class AuditEventDisplay
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string FormatElapsed(double elapsedSeconds)
    {
        var total = Math.Max(0, (int)Math.Round(elapsedSeconds));
        var minutes = total / 60;
        var seconds = total % 60;
        return $"{minutes}m {seconds:D2}s";
    }

    public static string FormatNodeLabel(string? nodeType, string? nodeTitle, Guid? missionNodeId = null)
    {
        var typeLabel = nodeType switch
        {
            "Trivia" => "Trivia",
            "TreasureHunt" => "Búsqueda del tesoro",
            _ when !string.IsNullOrWhiteSpace(nodeType) => nodeType!,
            _ => "Nodo"
        };

        if (!string.IsNullOrWhiteSpace(nodeTitle))
            return $"{typeLabel} \"{nodeTitle.Trim()}\"";

        if (missionNodeId is { } id && id != Guid.Empty)
            return $"{typeLabel} {id.ToString("N")[..8]}";

        return typeLabel;
    }

    public static string SerializeMetadata(object payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static (string Description, string Metadata) SessionStarted(int teamCount, DateTime startedAtUtc)
    {
        var description = "Sesión iniciada";
        var detail = $"{teamCount} {(teamCount == 1 ? "equipo" : "equipos")} · iniciada el {startedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}";
        return (
            description,
            SerializeMetadata(new
            {
                detail,
                teamCount,
                startedAtUtc
            }));
    }

    public static (string Description, string Metadata) SessionClosed(bool cancelled, DateTime finalizedAtUtc)
    {
        var description = cancelled ? "Sesión cancelada" : "Sesión finalizada";
        var verb = cancelled ? "Cancelada" : "Cerrada";
        var detail = $"{verb} el {finalizedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}";
        return (
            description,
            SerializeMetadata(new { detail, finalizedAtUtc, cancelled }));
    }

    public static (string Description, string Metadata) EvidenceValidated(
        string teamName,
        string nodeType,
        string nodeTitle,
        Guid missionNodeId,
        int score,
        double elapsedSeconds)
    {
        var description = $"Evidencia validada · {teamName}";
        var nodeLabel = FormatNodeLabel(nodeType, nodeTitle, missionNodeId);
        var detail = $"{nodeLabel} · +{score} pts · tiempo {FormatElapsed(elapsedSeconds)}";
        return (
            description,
            SerializeMetadata(new
            {
                detail,
                teamName,
                nodeType,
                nodeTitle,
                score,
                elapsedSeconds
            }));
    }

    public static (string Description, string Metadata) HintReleased(
        string teamName,
        string nodeType,
        string nodeTitle,
        Guid missionNodeId,
        Guid hintId,
        int hintOrder,
        int penaltyPoints,
        bool wasManualRelease)
    {
        var description = $"Pista liberada · {teamName}";
        var nodeLabel = FormatNodeLabel(nodeType, nodeTitle, missionNodeId);
        var releaseLabel = wasManualRelease ? "Manual" : "Automática";
        var hintPart = hintOrder > 0 ? $"pista #{hintOrder}" : "pista";
        var penaltyPart = penaltyPoints > 0
            ? $"-{penaltyPoints} pts"
            : "sin penalización de puntaje";
        var detail = $"{nodeLabel} · {hintPart} · {penaltyPart} · {releaseLabel}";
        return (
            description,
            SerializeMetadata(new
            {
                detail,
                teamName,
                nodeType,
                nodeTitle,
                hintId,
                hintOrder,
                penaltyPoints,
                wasManualRelease
            }));
    }

    public static (string Description, string Metadata) ManualPenalty(
        string teamName,
        int penaltyPoints,
        string reason,
        Guid operatorRef)
    {
        var description = $"Penalización manual · {teamName}";
        var detail = $"-{penaltyPoints} pts · Motivo: {reason}";
        return (
            description,
            SerializeMetadata(new
            {
                detail,
                teamName,
                penaltyPoints,
                reason,
                operatorRef
            }));
    }

    public static (string Description, string Metadata) TeamCompleted(
        string teamName,
        double elapsedSeconds,
        DateTime completedAtUtc)
    {
        var description = $"Misión completada · {teamName}";
        var detail = $"Tiempo total {FormatElapsed(elapsedSeconds)}";
        return (
            description,
            SerializeMetadata(new
            {
                detail,
                teamName,
                elapsedSeconds,
                completedAtUtc
            }));
    }

    public static (string Description, string Metadata) TeamRegistered(string teamName)
    {
        var description = $"Equipo registrado · {teamName}";
        const string detail = "Listo para la sesión";
        return (
            description,
            SerializeMetadata(new { detail, teamName }));
    }
}
