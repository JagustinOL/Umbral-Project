using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public class ActiveState : ISessionState
{
    public string Name => "Active";

    public void Start(LiveSession session) => throw new InvalidOperationException("La sesión ya está activa.");
    public void Pause(LiveSession session) => session.TransitionTo(new PausedState());
    public void Finish(LiveSession session) => session.TransitionTo(new CompletedState());

    public void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy)
    {
        session.AddEvidenceInternal(evidence);
        
        // Aplica el Patrón Strategy inyectado desde el caso de uso
        var pointsEarned = strategy.CalculatePoints(evidence);
        session.RewardTeamInternal(evidence.TeamId, pointsEarned);
    }
}