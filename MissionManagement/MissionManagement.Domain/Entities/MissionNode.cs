using Common;

namespace MissionManagement.Domain.Entities;

/// <summary>
/// Entidad que representa una Etapa o Nodo dentro de una Misión.
/// Implementa el patrón Composite: un nodo puede contener sub-nodos,
/// modelando estructuras jerárquicas (Etapa → Subetapa → Pista).
///
/// Tipos posibles: Stage (etapa principal), Trivia, TreasureHunt.
/// El tipo determina qué estrategia de cálculo usará ScoreCalculatorService.
/// </summary>
public sealed class MissionNode : Entity
{
    private readonly List<MissionNode> _children = [];
    private readonly List<Hint> _hints = [];

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public MissionNodeType NodeType { get; private set; }

    /// <summary>
    /// Orden de ejecución dentro de su contenedor padre.
    /// Determina la secuencia en que el equipo debe completar los nodos.
    /// </summary>
    public int ExecutionOrder { get; private set; }

    /// <summary>Puntaje base otorgado al completar este nodo correctamente.</summary>
    public int BaseScore { get; private set; }

    /// <summary>
    /// Nodo padre. Null si es nodo raíz de la misión (etapa de primer nivel).
    /// Patrón Composite: permite árbol de profundidad variable.
    /// </summary>
    public Guid? ParentNodeId { get; private set; }

    public IReadOnlyList<MissionNode> Children => _children.AsReadOnly();
    public IReadOnlyList<Hint> Hints => _hints.AsReadOnly();

    private MissionNode() { }

    public static MissionNode Create(
        string title,
        string description,
        MissionNodeType nodeType,
        int executionOrder,
        int baseScore,
        Guid? parentNodeId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título del nodo no puede estar vacío.", nameof(title));
        if (executionOrder < 1)
            throw new ArgumentOutOfRangeException(nameof(executionOrder), "El orden debe ser >= 1.");
        if (baseScore < 0)
            throw new ArgumentOutOfRangeException(nameof(baseScore), "El puntaje base no puede ser negativo.");

        return new MissionNode
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            NodeType = nodeType,
            ExecutionOrder = executionOrder,
            BaseScore = baseScore,
            ParentNodeId = parentNodeId
        };
    }

    /// <summary>
    /// Agrega un sub-nodo hijo (patrón Composite).
    /// Solo puede ser llamado desde Mission mientras está en Borrador.
    /// </summary>
    internal void AddChild(MissionNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        bool orderConflict = _children.Any(c => c.ExecutionOrder == child.ExecutionOrder);
        if (orderConflict)
            throw new InvalidOperationException(
                $"Ya existe un nodo hijo con ExecutionOrder={child.ExecutionOrder} en este nodo.");

        _children.Add(child);
    }

    /// <summary>
    /// Agrega una pista al nodo.
    /// Solo puede ser llamado desde Mission mientras está en Borrador.
    /// </summary>
    internal void AddHint(Hint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);

        bool orderConflict = _hints.Any(h => h.Order == hint.Order);
        if (orderConflict)
            throw new InvalidOperationException(
                $"Ya existe una pista con Order={hint.Order} en el nodo '{Title}'.");

        _hints.Add(hint);
    }

    /// <summary>
    /// Verifica si este nodo o cualquier descendiente contiene el nodeId dado.
    /// Usado por LiveSession para validar RB-05 (la evidencia pertenece a la misión).
    /// </summary>
    public bool ContainsNode(Guid nodeId)
    {
        if (Id == nodeId) return true;
        return _children.Any(child => child.ContainsNode(nodeId));
    }

    /// <summary>
    /// Retorna todos los IDs de nodos hoja (sin hijos) del subárbol.
    /// Estos son los nodos que los equipos pueden completar con evidencias.
    /// </summary>
    public IEnumerable<Guid> GetLeafNodeIds()
    {
        if (_children.Count == 0)
        {
            yield return Id;
            yield break;
        }

        foreach (var child in _children)
            foreach (var leafId in child.GetLeafNodeIds())
                yield return leafId;
    }
}