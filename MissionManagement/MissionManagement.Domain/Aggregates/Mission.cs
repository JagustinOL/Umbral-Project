using MissionManagement.Domain.Common;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Events;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Aggregates;

/// <summary>
/// AGGREGATE ROOT del Mission Design Context.
///
/// Responsabilidades:
/// — Gestionar el ciclo de vida de una Misión (Borrador → Activa → Inactiva).
/// — Proteger la estructura jerárquica de nodos (patrón Composite).
/// — Enforzar invariantes antes de activar la misión.
///
/// INVARIANTES (Reglas de Negocio protegidas):
/// — RB-01: Solo las misiones Activas pueden usarse para crear sesiones.
///          Se enforza aquí: Activate() valida que haya al menos un nodo.
///          LiveEngine valida el estado antes de crear una LiveSession.
/// — Una misión Activa no puede modificar su estructura de nodos ni pistas
///   (la plantilla del juego es inmutable una vez publicada).
/// </summary>
public sealed class Mission : AggregateRoot
{
    private readonly List<MissionNode> _nodes = [];
    private readonly List<OperatorRef> _operators = [];

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public MissionStatus Status { get; private set; }
    public DifficultyLevel Difficulty { get; private set; } = DifficultyLevel.Medium;

    /// <summary>
    /// Tiempo máximo en minutos para completar la misión.
    /// Null = sin límite de tiempo.
    /// </summary>
    public int? MaxDurationMinutes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastModifiedAtUtc { get; private set; }

    /// <summary>Nodos de primer nivel de la misión (raíces del árbol Composite).</summary>
    public IReadOnlyList<MissionNode> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<OperatorRef> Operators => _operators.AsReadOnly();

    private Mission() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva Misión en estado Borrador.
    /// Todas las misiones comienzan como Borrador hasta que el Administrador
    /// las activa explícitamente tras configurar sus nodos y pistas.
    /// </summary>
    public static Mission Create(
        string title,
        string description,
        DifficultyLevel difficulty,
        int? maxDurationMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título de la misión no puede estar vacío.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede estar vacía.", nameof(description));
        if (maxDurationMinutes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDurationMinutes),
                "La duración máxima debe ser mayor que cero si se especifica.");

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Difficulty = difficulty,
            MaxDurationMinutes = maxDurationMinutes,
            Status = MissionStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        return mission;
    }

    // ── Comportamiento ─────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega un nodo de primer nivel (etapa raíz) a la misión.
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador.
    /// </summary>
    public void AddRootNode(MissionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ThrowIfNotDraft("agregar nodos");

        if (node.ParentNodeId is not null)
            throw new InvalidOperationException(
                "Un nodo raíz no puede tener ParentNodeId. Use AddChildNode para sub-nodos.");


        if (node.NodeType != MissionNodeType.Stage)
            throw new InvalidOperationException(
                "Solo se pueden agregar nodos raíz de tipo 'Stage' (etapas) a la misión.");

        bool orderConflict = _nodes.Any(n => n.ExecutionOrder == node.ExecutionOrder);
        if (orderConflict)
            throw new InvalidOperationException(
                $"Ya existe un nodo raíz con ExecutionOrder={node.ExecutionOrder}.");

        node.AssignMission(Id);
        _nodes.Add(node);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Agrega un sub-nodo hijo a un nodo existente (patrón Composite).
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador.
    /// </summary>
    public void AddChildNode(Guid parentNodeId, MissionNode childNode)
    {
        ArgumentNullException.ThrowIfNull(childNode);
        ThrowIfNotDraft("agregar sub-nodos");

        var parent = FindNodeById(parentNodeId)
            ?? throw new InvalidOperationException(
                $"No se encontró el nodo padre con Id={parentNodeId}.");

        if (parent.NodeType != MissionNodeType.Stage)
            throw new InvalidOperationException(
                $"Solo los nodos de tipo 'Stage' pueden contener sub-nodos. " +
                $"El nodo '{parent.Title}' es de tipo '{parent.NodeType}'.");

        if (childNode.NodeType is not (MissionNodeType.Trivia or MissionNodeType.TreasureHunt))
            throw new InvalidOperationException(
                "Solo se pueden agregar sub-nodos de tipo 'Trivia' o 'TreasureHunt' a una etapa.");

        childNode.AssignMission(parent.MissionId);
        parent.AddChild(childNode);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public Guid AddTriviaNode(
        Guid parentNodeId,
        IReadOnlyList<TriviaQuestion> questions,
        int executionOrder,
        int baseScore = 0)
    {
        ThrowIfNotDraft("agregar juegos de trivia");
        var triviaNode = MissionNode.CreateTrivia(
            executionOrder,
            questions,
            parentNodeId,
            baseScore,
            missionId: Id);
        AddChildNode(parentNodeId, triviaNode);
        return triviaNode.Id;
    }

    public Guid AddTreasureHuntNode(
        Guid parentNodeId,
        string instructions,
        string secretCode,
        GpsCoordinate destination,
        int executionOrder,
        int baseScore = 0)
    {
        ThrowIfNotDraft("agregar juegos de busqueda");
        var treasureNode = MissionNode.CreateTreasureHunt(
            executionOrder,
            instructions,
            secretCode,
            destination,
            parentNodeId,
            baseScore,
            missionId: Id);

        AddChildNode(parentNodeId, treasureNode);
        return treasureNode.Id;
    }

    /// <summary>
    /// Agrega una pista a un nodo existente.
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador.
    /// </summary>
    public void AddHintToNode(Guid nodeId, Hint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);
        ThrowIfNotDraft("agregar pistas");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        node.AddHint(hint);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza una etapa (nodo) existente.
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador (RN-01).
    /// </summary>
    public void UpdateNode(Guid nodeId, string title, string description)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacío.", nameof(nodeId));

        ThrowIfNotDraft("editar etapas");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        if (node.NodeType != MissionNodeType.Stage)
            throw new InvalidOperationException("Solo se pueden editar nodos de tipo 'Stage' en esta épica.");

        node.UpdateDetails(title, description);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateTriviaNode(Guid nodeId, IReadOnlyList<TriviaQuestion> questions)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacio.", nameof(nodeId));

        ThrowIfNotDraft("editar trivias");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        node.UpdateTriviaQuestions(questions);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateTreasureHuntNode(
        Guid nodeId,
        string instructions,
        string secretCode,
        GpsCoordinate destination)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacio.", nameof(nodeId));

        ThrowIfNotDraft("editar busquedas");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        node.UpdateTreasureHunt(instructions, secretCode, destination);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Elimina una etapa (nodo) existente.
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador (RN-01).
    /// </summary>
    public void DeleteNode(Guid nodeId)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacío.", nameof(nodeId));

        ThrowIfNotDraft("eliminar etapas");

        var existing = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        if (existing.ParentNodeId is null)
        {
            var root = _nodes.FirstOrDefault(n => n.Id == nodeId);
            if (root is null)
                throw new InvalidOperationException($"No se encontró el nodo raíz con Id={nodeId}.");

            _nodes.Remove(root);
        }
        else
        {
            var parent = FindNodeById(existing.ParentNodeId.Value)
                ?? throw new InvalidOperationException(
                    $"No se encontró el nodo padre con Id={existing.ParentNodeId}.");

            var removed = parent.RemoveChild(nodeId);
            if (!removed)
                throw new InvalidOperationException(
                    $"El nodo padre con Id={existing.ParentNodeId} no contiene el hijo con Id={nodeId}.");
        }

        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateHint(Guid nodeId, Guid hintId, string newContent)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacio.", nameof(nodeId));
        if (hintId == Guid.Empty)
            throw new ArgumentException("El hintId no puede ser vacio.", nameof(hintId));

        ThrowIfNotDraft("editar pistas");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");
        var hint = node.FindHint(hintId)
            ?? throw new InvalidOperationException($"No se encontró la pista con Id={hintId}.");

        hint.UpdateContent(newContent);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void DeleteHint(Guid nodeId, Guid hintId)
    {
        if (nodeId == Guid.Empty)
            throw new ArgumentException("El nodeId no puede ser vacio.", nameof(nodeId));
        if (hintId == Guid.Empty)
            throw new ArgumentException("El hintId no puede ser vacio.", nameof(hintId));

        ThrowIfNotDraft("eliminar pistas");

        var node = FindNodeById(nodeId)
            ?? throw new InvalidOperationException($"No se encontró el nodo con Id={nodeId}.");

        var removed = node.RemoveHint(hintId);
        if (!removed)
            throw new InvalidOperationException($"No se encontró la pista con Id={hintId}.");

        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void AssignOperator(Guid operatorId)
    {
        var operatorRef = new OperatorRef(operatorId);
        if (_operators.Any(x => x.OperatorId == operatorRef.OperatorId))
            throw new InvalidOperationException(
                $"El operador con Id={operatorRef.OperatorId} ya está asignado a la misión '{Title}'.");

        _operators.Add(operatorRef);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void RevokeOperator(Guid operatorId)
    {
        if (operatorId == Guid.Empty)
            throw new ArgumentException("El operatorId no puede ser vacio.", nameof(operatorId));

        var existing = _operators.FirstOrDefault(x => x.OperatorId == operatorId);
        if (existing is null)
            throw new InvalidOperationException(
                $"El operador con Id={operatorId} no está asignado a la misión '{Title}'.");

        _operators.Remove(existing);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Activa la misión, haciéndola disponible para crear sesiones en LiveEngine.
    ///
    /// INVARIANTE RB-01: Una misión no puede activarse si no tiene al menos un MissionNode.
    /// Una vez Activa, su estructura es inmutable (la plantilla del juego está "publicada").
    ///
    /// Dispara: MissionActivatedEvent — LiveEngine lo consume para registrar
    /// los AllowedNodeIds y resolver RB-05 de forma asíncrona.
    /// </summary>
    public void Activate()
    {
        if (Status == MissionStatus.Active)
            throw new InvalidOperationException(
                $"La misión '{Title}' ya está activa.");

        if (Status == MissionStatus.Inactive)
            throw new InvalidOperationException(
                $"Una misión inactiva no puede reactivarse directamente. " +
                $"Crea una nueva versión si deseas reutilizarla.");

        // INVARIANTE RB-01: debe tener al menos un nodo
        if (_nodes.Count == 0)
            throw new InvalidOperationException(
                $"La misión '{Title}' no puede activarse porque no tiene ningún nodo (etapa) configurado. " +
                $"Agrega al menos un MissionNode antes de activar.");

        Status = MissionStatus.Active;
        LastModifiedAtUtc = DateTime.UtcNow;

        // Construir snapshot de nodos hoja para el evento
        var allowedNodes = _nodes
            .SelectMany(n => n.GetLeafNodeIds()
                .Select(id => new ActivatedNodeSnapshot(
                    NodeId: id,
                    NodeType: FindNodeById(id)!.NodeType.ToString(),
                    BaseScore: FindNodeById(id)!.BaseScore)))
            .ToList();

        RaiseDomainEvent(new MissionActivatedEvent
        {
            MissionId = Id,
            MissionTitle = Title,
            AllowedNodes = allowedNodes,
            DifficultyMultiplier = Difficulty.ScoreMultiplier
        });
    }

    /// <summary>
    /// Desactiva la misión. Las sesiones existentes no se ven afectadas,
    /// pero no podrán crearse nuevas sesiones con esta misión.
    /// </summary>
    public void Deactivate()  //ELIMINAR
    {
        if (Status == MissionStatus.Inactive)
            throw new InvalidOperationException($"La misión '{Title}' ya está inactiva.");

        Status = MissionStatus.Inactive;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza los metadatos básicos de la misión.
    /// INVARIANTE: Solo permitido mientras la misión está en estado Borrador.
    /// </summary>
    public void UpdateDetails(string title, string description, int? maxDurationMinutes)  //MODIFICAR MISION
    {
        ThrowIfNotDraft("editar detalles");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título no puede estar vacío.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción no puede estar vacía.", nameof(description));
        if (maxDurationMinutes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDurationMinutes),
                "La duración máxima debe ser mayor que cero si se especifica.");

        Title = title;
        Description = description;
        MaxDurationMinutes = maxDurationMinutes;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    // ── Consultas internas ─────────────────────────────────────────────────────

    /// <summary>
    /// Retorna todos los IDs de nodos hoja válidos para recibir evidencias.
    /// Usado al construir LiveSession para resolver RB-05 localmente.
    /// </summary>
    public IReadOnlySet<Guid> GetAllowedNodeIds() =>
        _nodes
            .SelectMany(n => n.GetLeafNodeIds())
            .ToHashSet();

    /// <summary>
    /// Busca un nodo en el árbol completo por su Id (búsqueda en profundidad).
    /// </summary>
    public MissionNode? FindNodeById(Guid nodeId) =>
        _nodes
            .Select(n => FindInSubtree(n, nodeId))
            .FirstOrDefault(n => n is not null);

    // ── Helpers privados ───────────────────────────────────────────────────────

    private void ThrowIfNotDraft(string operation)
    {
        if (Status != MissionStatus.Draft)
            throw new InvalidOperationException(
                $"No se puede {operation} en la misión '{Title}' " +
                $"porque su estado actual es '{Status}'. " +
                $"Solo las misiones en Borrador pueden modificarse.");
    }

    private static MissionNode? FindInSubtree(MissionNode node, Guid targetId)
    {
        if (node.Id == targetId) return node;
        return node.Children
            .Select(child => FindInSubtree(child, targetId))
            .FirstOrDefault(found => found is not null);
    }
}