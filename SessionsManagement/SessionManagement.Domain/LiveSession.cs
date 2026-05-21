using SessionManagement.Domain.Common;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.States;
using SessionManagement.Domain.Strategies;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain;

public class LiveSession : AggregateRoot
{
    public Guid MissionId { get; private set; }
    public Guid OperatorId { get; private set; }
    public SessionCode Code { get; private set; }

    private ISessionState _currentState;
    public string CurrentStateName => _currentState.Name;

    private readonly List<TeamSessionProgress> _teamProgresses = [];
    public IReadOnlyCollection<TeamSessionProgress> TeamProgresses => _teamProgresses.AsReadOnly();

    private readonly List<EvidenceSubmission> _evidences = [];
    public IReadOnlyCollection<EvidenceSubmission> Evidences => _evidences.AsReadOnly();

    private LiveSession(Guid id, Guid missionId, Guid operatorId)
    {
        Id = id;
        MissionId = missionId;
        OperatorId = operatorId;
        Code = SessionCode.Generate();
        _currentState = new ScheduledState();
    }

    public static LiveSession Create(Guid missionId, Guid operatorId)
    {
        return new LiveSession(Guid.NewGuid(), missionId, operatorId);
    }

    public void RegisterTeam(Guid teamId)
    {
        if (_currentState is not ScheduledState)
            throw new InvalidOperationException("Solo se pueden registrar equipos antes de iniciar la partida.");

        if (!_teamProgresses.Any(p => p.TeamId == teamId))
            _teamProgresses.Add(new TeamSessionProgress(teamId));
    }

    // --- MÉTODOS DE COMPORTAMIENTO (DELEGADOS AL STATE) ---
    public void StartGame() => _currentState.Start(this);
    public void PauseGame() => _currentState.Pause(this);
    public void FinishGame() => _currentState.Finish(this);

    public void ProcessEvidence(Guid teamId, Guid stageId, string payload, bool isValid, IScoreStrategy strategy)
    {
        var evidence = new EvidenceSubmission(Guid.NewGuid(), teamId, stageId, payload, isValid);
        _currentState.AcceptEvidence(this, evidence, strategy);
    }

    public void ApplyManualPenalty(Guid teamId, Points points, PenaltyReason reason)
    {
        if (_currentState is not ActiveState)
            throw new InvalidOperationException("Solo se pueden aplicar penalizaciones en una sesión activa.");

        var progress = _teamProgresses.FirstOrDefault(p => p.TeamId == teamId)
            ?? throw new InvalidOperationException("El equipo no pertenece a esta sesión.");

        progress.DeductPoints(points);
    }

    // --- MÉTODOS INTERNOS DE TRANSICIÓN Y MODIFICACIÓN ---
    internal void TransitionTo(ISessionState newState)
    {
        _currentState = newState;
    }

    internal void AddEvidenceInternal(EvidenceSubmission evidence)
    {
        _evidences.Add(evidence);
    }

    internal void RewardTeamInternal(Guid teamId, Points points)
    {
        var progress = _teamProgresses.FirstOrDefault(p => p.TeamId == teamId);
        progress?.AddPoints(points);
    }
}