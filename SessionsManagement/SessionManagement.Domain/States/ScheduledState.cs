using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public class ScheduledState : ISessionState
{
    public string Name => "Scheduled";

    public void Start(LiveSession session)
    {
        if (session.TeamProgresses.Count == 0)
            throw new InvalidOperationException("RN-15: Una sesión no puede iniciar si no posee al menos un equipo registrado.");

        session.TransitionTo(new ActiveState());
    }

    public void Pause(LiveSession session) => throw new InvalidOperationException("No se puede pausar una sesión programada.");
    public void Finish(LiveSession session) => session.TransitionTo(new CancelledState());
    public void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy) 
        => throw new InvalidOperationException("La sesión no ha comenzado.");
}