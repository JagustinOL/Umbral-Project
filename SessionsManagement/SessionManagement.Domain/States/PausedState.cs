using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public class PausedState : ISessionState
{
    public string Name => "Paused";

    public void Start(LiveSession session) => session.TransitionTo(new ActiveState()); // Reanudar
    public void Pause(LiveSession session) => throw new InvalidOperationException("La sesión ya está pausada.");
    public void Finish(LiveSession session) => session.TransitionTo(new CompletedState());
    
    public void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy) 
        => throw new InvalidOperationException("RN-03: El sistema debe rechazar evidencias si la sesión está Pausada.");
}