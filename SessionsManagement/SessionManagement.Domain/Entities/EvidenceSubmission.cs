using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Entities;

public class EvidenceSubmission : Entity
{
    public Guid TeamId { get; private set; }
    public Guid StageId { get; private set; }
    public string Payload { get; private set; }
    public bool IsValid { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    internal EvidenceSubmission(Guid id, Guid teamId, Guid stageId, string payload, bool isValid)
    {
        Id = id;
        TeamId = teamId;
        StageId = stageId;
        Payload = payload;
        IsValid = isValid;
        SubmittedAt = DateTime.UtcNow;
    }
}