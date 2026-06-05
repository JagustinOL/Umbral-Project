namespace SessionManagement.Domain.ValueObjects;

/// <summary>
/// Value Object que representa un snapshot de un nodo de misión
/// permitido dentro de una LiveSession.
///
/// Resuelve RB-05 localmente: cuando LiveSession recibe una evidencia,
/// verifica que el NodeId pertenezca a su colección de AllowedNodes
/// sin necesidad de llamar síncronamente al MissionManagement context.
///
/// Se carga al crear la sesión a partir del evento MissionActivatedEvent
/// o desde los datos de la misión activa en el momento de la creación.
/// </summary>
public sealed record AllowedNode(
    Guid NodeId,
    string NodeType,
    int BaseScore
)
{
    public AllowedNode() : this(Guid.Empty, string.Empty, 0) { }
}