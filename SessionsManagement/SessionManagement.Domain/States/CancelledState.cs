using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public class CancelledState : ISessionState
{
    public string Name => "Cancelled";

    public void Start(LiveSession session) 
        => throw new InvalidOperationException("RN-17: Una sesión cancelada es inmutable y no puede ser iniciada.");
        
    public void Pause(LiveSession session) 
        => throw new InvalidOperationException("RN-17: Una sesión cancelada no puede ser pausada.");
        
    public void Finish(LiveSession session) 
        => throw new InvalidOperationException("La sesión fue cancelada, no puede marcarse como finalizada con éxito.");
        
    public void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy) 
        => throw new InvalidOperationException("RN-03: No se deben aceptar evidencias si la sesión está cancelada.");
}