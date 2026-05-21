namespace AdminService.Domain.Entities;

public class StageNode : MissionNode
{
    private readonly List<IMissionComponent> _children = [];
    public IReadOnlyCollection<IMissionComponent> Children => _children.AsReadOnly();  //PREGUNTAR

    private StageNode(Guid id, Guid missionId, string title, int order)
    {
        Id = id;
        MissionId = missionId;
        Title = title;
        Order = order;
    }

    public static StageNode Create(Guid missionId, string title, int order)
    {
        return new StageNode(Guid.NewGuid(), missionId, title, order);
    }

    public void AddChild(IMissionComponent child)
    {
        _children.Add(child);
    }

    public override TimeSpan CalculateEstimatedTime()
    {
        long totalTicks = _children.Sum(c => c.CalculateEstimatedTime().Ticks);
        return TimeSpan.FromTicks(totalTicks);
    }
}