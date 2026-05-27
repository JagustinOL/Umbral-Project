using MissionManagement.Domain.Aggregates;

namespace MissionManagement.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para el Aggregate Mission.
///
/// PRINCIPIO: Esta interfaz vive en el Dominio — define QUÉ se necesita.
/// La implementación (MissionPostgresRepo) vive en Infrastructure — define CÓMO.
/// Esto garantiza que el Dominio no depende de EF Core ni de PostgreSQL (DIP).
///
/// Nota sobre carga del árbol:
/// GetByIdAsync debe cargar el árbol completo (Mission + Nodes + Hints)
/// en una sola operación, ya que el Aggregate Root es la única entrada
/// y el dominio necesita todos sus hijos para enforcar invariantes.
/// </summary>
public interface IMissionRepository
{
    /// <summary>
    /// Carga una misión con su árbol completo de nodos y pistas.
    /// Retorna null si no existe.
    /// </summary>
    Task<Mission?> GetByIdAsync(Guid missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todas las misiones en estado Active.
    /// Usado por el Administrador para seleccionar la misión al crear una sesión (RF-03).
    /// </summary>
    Task<IReadOnlyList<Mission>> GetActiveMissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste una misión nueva o actualiza una existente.
    /// El repositorio determina si es INSERT o UPDATE por la existencia del Id.
    /// </summary>
    Task SaveAsync(Mission mission, CancellationToken cancellationToken = default);
}