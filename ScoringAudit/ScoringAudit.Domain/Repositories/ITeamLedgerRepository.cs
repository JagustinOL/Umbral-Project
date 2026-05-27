using ScoringAudit.Domain.Aggregates;

namespace ScoringAudit.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para TeamLedger.
///
/// GetByTeamAndSessionAsync carga el agregado completo con todos
/// sus ScoreEntries — sin ellos, RB-07 no puede garantizarse
/// (TotalScore = suma de todo el historial).
/// </summary>
public interface ITeamLedgerRepository
{
    Task<TeamLedger?> GetByTeamAndSessionAsync(
        Guid teamRef,
        Guid sessionRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga todos los TeamLedgers de una sesión.
    /// Necesario para RankingManagerService, que compara todos los equipos.
    /// </summary>
    Task<IReadOnlyList<TeamLedger>> GetBySessionAsync(
        Guid sessionRef,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        TeamLedger ledger,
        CancellationToken cancellationToken = default);
}