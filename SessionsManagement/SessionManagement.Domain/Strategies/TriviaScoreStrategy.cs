using SessionManagement.Domain.Entities;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain.Strategies;

public class TriviaScoreStrategy : IScoreStrategy
{
    public Points CalculatePoints(EvidenceSubmission evidence) 
        => evidence.IsValid ? new Points(100) : Points.Zero;
}