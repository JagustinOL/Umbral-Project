namespace SessionManagement.Domain.Aggregates;

/// <summary>
/// Estados del ciclo de vida de una LiveSession.
///
/// Transiciones válidas (Patrón State — RB-09):
///
///   Pending ──► Preparation ──► Active ──► Paused ──► Active
///                                         └──► Finalized
///                                         └──► Cancelled
///   Preparation ──► Cancelled
///   Pending   ──► Cancelled
///
/// Cualquier otra transición lanza SessionDomainException.
/// </summary>
public enum LiveSessionStatus
{
    /// <summary>Creada, aún no está configurando equipos.</summary>
    Pending = 0,

    /// <summary>En preparación: equipos registrándose, configuración final.</summary>
    Preparation = 1,

    /// <summary>Juego activo. Se aceptan evidencias (RB-03).</summary>
    Active = 2,

    /// <summary>Pausada temporalmente. No se aceptan evidencias (RB-03).</summary>
    Paused = 3,

    /// <summary>Concluida normalmente. Estado terminal.</summary>
    Finalized = 4,

    /// <summary>Cancelada. Estado terminal.</summary>
    Cancelled = 5
}