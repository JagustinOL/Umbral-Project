using SessionManagement.Domain.Common;
using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Domain.Entities;

/// <summary>
/// Entidad que representa una evidencia o respuesta enviada
/// por un equipo para completar un nodo de la misión.
///
/// RB-05: Cada evidencia debe asociarse exactamente a un equipo,
/// una sesión y una etapa. La sesión que la contiene provee el
/// SessionId implícitamente.
///
/// RB-09 (RF-09): Cada envío queda registrado con fecha, equipo,
/// sesión y estado de validación. Esta entidad es el registro inmutable.
///
/// El campo IsValid se establece UNA SOLA VEZ. Una evidencia no puede
/// re-validarse ni modificarse. En Trivia, un intento incorrecto cierra
/// el nodo (sin reintento); en Búsqueda del Tesoro el equipo puede
/// enviar una nueva evidencia hasta acertar el código.
/// </summary>
public sealed class EvidenceSubmission : Entity
{
    public Guid TeamId { get; private set; }

    /// <summary>
    /// Nodo de la misión al que responde esta evidencia.
    /// Validado contra LiveSession.AllowedNodes al momento del envío (RB-05).
    /// </summary>
    public Guid MissionNodeId { get; private set; }

    /// <summary>
    /// Contenido de la respuesta. Puede ser texto libre, URL de imagen,
    /// código QR, etc. — depende del tipo de nodo.
    /// </summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// Índice de pregunta trivia respondida. Null para búsqueda del tesoro u otros tipos.
    /// </summary>
    public int? QuestionIndex { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    /// <summary>
    /// Null = pendiente de validación.
    /// True = validada y aprobada.
    /// False = rechazada.
    /// </summary>
    public bool? IsValid { get; private set; }

    public DateTime? ValidatedAtUtc { get; private set; }

    /// <summary>Motivo del rechazo si IsValid = false.</summary>
    public string? RejectionReason { get; private set; }

    private EvidenceSubmission() { }

    /// <summary>
    /// Crea una nueva evidencia en estado pendiente de validación.
    /// Llamado exclusivamente desde LiveSession.AcceptEvidence().
    /// </summary>
    internal static EvidenceSubmission Create(
        Guid teamId,
        Guid missionNodeId,
        string payload,
        int? questionIndex = null)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));
        if (missionNodeId == Guid.Empty)
            throw new ArgumentException("MissionNodeId no puede ser vacío.", nameof(missionNodeId));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("El payload de la evidencia no puede estar vacío.", nameof(payload));
        if (questionIndex is < 0)
            throw new ArgumentOutOfRangeException(nameof(questionIndex), "QuestionIndex no puede ser negativo.");

        return new EvidenceSubmission
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            MissionNodeId = missionNodeId,
            Payload = payload.Trim(),
            QuestionIndex = questionIndex,
            SubmittedAtUtc = DateTime.UtcNow,
            IsValid = null
        };
    }

    /// <summary>
    /// Marca la evidencia como válida.
    /// INVARIANTE: Solo puede validarse una vez (IsValid era null).
    /// </summary>
    internal void MarkAsValid()
    {
        if (IsValid.HasValue)
            throw new SessionDomainException(
                $"La evidencia {Id} ya fue procesada (IsValid={IsValid}). " +
                "No puede revalidarse.");

        IsValid = true;
        ValidatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca la evidencia como inválida con un motivo obligatorio.
    /// INVARIANTE: Solo puede rechazarse una vez (IsValid era null).
    /// </summary>
    internal void MarkAsInvalid(string reason)
    {
        if (IsValid.HasValue)
            throw new SessionDomainException(
                $"La evidencia {Id} ya fue procesada (IsValid={IsValid}). " +
                "No puede rechazarse de nuevo.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Se debe proporcionar un motivo para rechazar una evidencia.", nameof(reason));

        IsValid = false;
        RejectionReason = reason;
        ValidatedAtUtc = DateTime.UtcNow;
    }
}