using SessionManagement.Domain.Entities;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain.Strategies;

public interface IScoreStrategy
{
    Points CalculatePoints(EvidenceSubmission evidence);
}