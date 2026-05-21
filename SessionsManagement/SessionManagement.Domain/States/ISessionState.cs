using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Strategies;

namespace SessionManagement.Domain.States;

public interface ISessionState
{
    string Name { get; }
    void Start(LiveSession session);
    void Pause(LiveSession session);
    void Finish(LiveSession session);
    void AcceptEvidence(LiveSession session, EvidenceSubmission evidence, IScoreStrategy strategy);
}