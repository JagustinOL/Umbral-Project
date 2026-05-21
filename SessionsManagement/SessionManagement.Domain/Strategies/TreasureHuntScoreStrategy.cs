using SessionManagement.Domain.Entities;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain.Strategies;

public class TreasureHuntScoreStrategy : IScoreStrategy
{
    public Points CalculatePoints(EvidenceSubmission evidence) 
        => evidence.IsValid ? new Points(250) : Points.Zero;
}