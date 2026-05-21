using AdminService.Domain.Common;

namespace AdminService.Domain.Entities;

public abstract class MissionNode : Entity, IMissionComponent
{
    public Guid MissionId { get; protected set; }
    public string Title { get; protected set; }
    public int Order { get; protected set; }  //PREGUNTAR

    public abstract TimeSpan CalculateEstimatedTime();
}