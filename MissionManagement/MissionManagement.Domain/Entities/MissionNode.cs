using Common;
using MissionManagement.Domain.ValueObjects;

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
    public string? Instructions { get; private set; }
    public string? SecretCode { get; private set; }
    public GpsCoordinate? Destination { get; private set; }
    public List<TriviaQuestion> TriviaQuestions { get; private set; } = [];

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
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del nodo no puede estar vacía.", nameof(description));
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

    public static MissionNode CreateTrivia(
        int executionOrder,
        IReadOnlyList<TriviaQuestion> questions,
        Guid parentNodeId,
        int baseScore = 0)
    {
        if (parentNodeId == Guid.Empty)
            throw new ArgumentException("El parentNodeId no puede ser vacio.", nameof(parentNodeId));
        if (questions is null || questions.Count == 0)
            throw new ArgumentException("La trivia debe tener al menos una pregunta.", nameof(questions));

        var node = Create(
            title: "Trivia",
            description: "Trivia challenge",
            nodeType: MissionNodeType.Trivia,
            executionOrder: executionOrder,
            baseScore: baseScore,
            parentNodeId: parentNodeId);

        node.TriviaQuestions = questions.ToList();
        return node;
    }

    public static MissionNode CreateTreasureHunt(
        int executionOrder,
        string instructions,
        string secretCode,
        GpsCoordinate destination,
        Guid parentNodeId,
        int baseScore = 0)
    {
        if (parentNodeId == Guid.Empty)
            throw new ArgumentException("El parentNodeId no puede ser vacio.", nameof(parentNodeId));
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Las instrucciones no pueden estar vacias.", nameof(instructions));
        if (string.IsNullOrWhiteSpace(secretCode))
            throw new ArgumentException("El codigo secreto no puede estar vacio.", nameof(secretCode));
        ArgumentNullException.ThrowIfNull(destination);

        var node = Create(
            title: "Treasure Hunt",
            description: "Treasure hunt challenge",
            nodeType: MissionNodeType.TreasureHunt,
            executionOrder: executionOrder,
            baseScore: baseScore,
            parentNodeId: parentNodeId);

        node.Instructions = instructions.Trim();
        node.SecretCode = secretCode.Trim();
        node.Destination = destination;
        return node;
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

    internal void UpdateDetails(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título del nodo no puede estar vacío.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del nodo no puede estar vacía.", nameof(description));

        Title = title;
        Description = description;
    }

    internal void UpdateTriviaQuestions(IReadOnlyList<TriviaQuestion> questions)
    {
        if (NodeType != MissionNodeType.Trivia)
            throw new InvalidOperationException("Solo los nodos Trivia pueden actualizar preguntas.");
        if (questions is null || questions.Count == 0)
            throw new ArgumentException("La trivia debe mantener al menos una pregunta.", nameof(questions));

        TriviaQuestions = questions.ToList();
    }

    internal void UpdateTreasureHunt(string instructions, string secretCode, GpsCoordinate destination)
    {
        if (NodeType != MissionNodeType.TreasureHunt)
            throw new InvalidOperationException("Solo los nodos TreasureHunt pueden actualizarse con coordenadas.");
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Las instrucciones no pueden estar vacias.", nameof(instructions));
        if (string.IsNullOrWhiteSpace(secretCode))
            throw new ArgumentException("El codigo secreto no puede estar vacio.", nameof(secretCode));
        ArgumentNullException.ThrowIfNull(destination);

        Instructions = instructions.Trim();
        SecretCode = secretCode.Trim();
        Destination = destination;
    }

    internal Hint? FindHint(Guid hintId) => _hints.FirstOrDefault(h => h.Id == hintId);

    internal bool RemoveHint(Guid hintId)
    {
        var hint = _hints.FirstOrDefault(h => h.Id == hintId);
        if (hint is null)
            return false;

        _hints.Remove(hint);
        return true;
    }

    internal bool RemoveChild(Guid childNodeId)
    {
        var child = _children.FirstOrDefault(c => c.Id == childNodeId);
        if (child is null)
            return false;

        _children.Remove(child);
        return true;
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