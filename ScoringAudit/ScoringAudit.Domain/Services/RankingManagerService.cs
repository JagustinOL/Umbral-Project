using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Services;

/// <summary>
/// Domain Service que construye el ranking de una sesión.
///
/// ¿Por qué no está en TeamLedger?
/// Porque un TeamLedger solo conoce los puntos de UN equipo.
/// Para armar un ranking hay que comparar TODOS los TeamLedgers
/// de la sesión, lo que requiere orquestar múltiples agregados.
///
/// RB-08: El ranking debe ordenarse de mayor a menor puntaje.
/// En caso de empate, gana quien tenga menor TotalElapsedSeconds
/// (completó los mismos puntos en menos tiempo).
///
/// Este servicio es STATELESS — recibe los datos, calcula y devuelve.
/// No persiste nada directamente; el Application Service persiste
/// y dispara TeamScoreUpdatedEvent con el resultado.
/// </summary>
public sealed class RankingManagerService
{
    /// <summary>
    /// Genera el ranking ordenado a partir de una colección de TeamLedgers.
    ///
    /// Llamado por GetLiveRankingQueryHandler (consulta CQRS) y también
    /// por ProcessEvidenceValidatedEventHandler tras actualizar el ledger,
    /// para que el resultado viaje en TeamScoreUpdatedEvent hacia SignalR.
    /// </summary>
    public IReadOnlyList<RankingEntry> BuildRanking(IEnumerable<TeamLedger> ledgers)
    {
        ArgumentNullException.ThrowIfNull(ledgers);

        var sorted = ledgers
            // RB-08: orden primario = mayor puntaje primero
            .OrderByDescending(l => l.TotalScore)
            // RB-08: criterio de desempate = menor tiempo (más rápido)
            .ThenBy(l => l.LastPositiveEntryElapsedSeconds)
            .ToList();

        return sorted
            .Select((ledger, index) => new RankingEntry(
                Position: index + 1,
                TeamId: ledger.TeamRef,
                TeamName: ledger.TeamName,
                TotalScore: ledger.TotalScore,
                TotalElapsedSeconds: ledger.LastPositiveEntryElapsedSeconds,
                CompletedNodes: ledger.CompletedNodesCount,
                PenaltiesApplied: ledger.PenaltiesCount
            ))
            .ToList()
            .AsReadOnly();
    }
}