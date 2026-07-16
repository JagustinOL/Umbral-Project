namespace ScoringAudit.Domain.Entities;

/// <summary>
/// Tipos de eventos auditables en el ciclo de vida de una sesión.
/// Usado por AuditLog para categorizar el historial (RF-15).
/// </summary>
public enum SessionEventType
{
    SessionStarted       = 0,
    SessionPaused        = 1,
    SessionResumed       = 2,
    SessionFinalized     = 3,
    SessionCancelled     = 4,
    EvidenceValidated    = 5,
    EvidenceRejected     = 6,
    HintReleased         = 7,
    ManualPenaltyApplied = 8,
    TeamRegistered       = 9,
    ScoreRecalculated    = 10,
    TeamCompletedMission = 11
}