using AdminService.Domain.Common;
using AdminService.Domain.ValueObjects;

namespace AdminService.Domain.Entities;

public class Mission : AggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<StageNode> _rootStages = [];
    public IReadOnlyCollection<StageNode> RootStages => _rootStages.AsReadOnly();

    private Mission(Guid id, string title, string description, DifficultyLevel difficulty)
    {
        Id = id;
        Title = title;
        Description = description;
        Difficulty = difficulty;
        IsActive = false; 
    }

    public static Mission CreateDraft(string title, string description, DifficultyLevel difficulty)
    {
        return new Mission(Guid.NewGuid(), title, description, difficulty);
    }

    public void AddRootStage(StageNode stage)
    {
        if (IsActive)
            throw new InvalidOperationException("No se pueden agregar etapas a una misión activa."); 
            
        _rootStages.Add(stage);
    }

    public void Activate()
    {
        if (_rootStages.Count == 0)
            throw new InvalidOperationException("La misión debe tener al menos una etapa para ser activada.");
            
        IsActive = true;
    }

    public TimeSpan GetTotalEstimatedTime()
    {
        long totalTicks = _rootStages.Sum(s => s.CalculateEstimatedTime().Ticks);
        return TimeSpan.FromTicks(totalTicks);
    }
}