namespace SessionManagement.Domain.Entities;

/// <summary>
/// Estado de participación de un equipo registrado dentro de una LiveSession.
/// </summary>
public enum TeamParticipationStatus
{
    Active = 0,
    Completed = 1,
    Expelled = 2
}
