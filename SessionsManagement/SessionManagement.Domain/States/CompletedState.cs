using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public class CompletedState : ISessionState
{
    public string Name => "Completed";

    public void Start(LiveSession session) 
        => throw new InvalidOperationException("RN-17: La sesión ya finalizó con éxito y no puede ser reiniciada.");
        
    public void Pause(LiveSession session) 
        => throw new InvalidOperationException("RN-17: La sesión finalizada es inmutable y no puede ser pausada.");
        
    public void Finish(LiveSession session) 
        => throw new InvalidOperationException("La sesión ya se encuentra en estado finalizado.");
        
    public void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy) 
        => throw new InvalidOperationException("RN-03: No se deben aceptar evidencias si la sesión está finalizada.");
}